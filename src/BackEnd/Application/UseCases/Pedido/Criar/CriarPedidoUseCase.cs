using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Services.ClienteLogado;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.Criar
{
    // Cria o pedido como AguardandoPagamento. O estoque só é conferido aqui (não é reservado):
    // a baixa acontece quando o admin confirma o pagamento.
    public class CriarPedidoUseCase : ICriarPedidoUseCase
    {
        // maior valor que cabe em pedidos.total (numeric(10,2))
        private const decimal TOTAL_MAXIMO = 99_999_999.99m;

        private readonly IClienteLogado _clienteLogado;
        private readonly IProdutoVarianteRepository _varianteRepository;
        private readonly IProdutoOpcaoRepository _opcaoRepository;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public CriarPedidoUseCase(IClienteLogado clienteLogado, IProdutoVarianteRepository varianteRepository, IProdutoOpcaoRepository opcaoRepository, IPedidoRepository pedidoRepository, ISalvarDBRepository salvarDB)
        {
            _clienteLogado = clienteLogado;
            _varianteRepository = varianteRepository;
            _opcaoRepository = opcaoRepository;
            _pedidoRepository = pedidoRepository;
            _salvarDB = salvarDB;
        }

        public async Task<ResponsePedido> Execute(RequestCriarPedido request)
        {
            var cliente = await _clienteLogado.Cliente();

            Validate(request);

            var variantes = (await _varianteRepository.ListarComEstoquePorIds(request.Itens.Select(item => item.VarianteId).Distinct().ToList()))
                .ToDictionary(variante => variante.Id);

            var opcoes = (await _opcaoRepository.ListarPorIds(request.Itens.SelectMany(item => item.PersonalizacaoIds ?? []).Distinct().ToList()))
                .ToDictionary(opcao => opcao.Id);

            ValidarItens(request, variantes, opcoes);
            ValidarEstoque(request, variantes);

            var pedido = new Pedidos
            {
                ClienteId = cliente.Id,
                Status = StatusPedido.AguardandoPagamento,
                Observacao = string.IsNullOrWhiteSpace(request.Observacao) ? null : request.Observacao.Trim()
            };

            foreach (var itemRequest in request.Itens)
            {
                var variante = variantes[itemRequest.VarianteId];
                var personalizacoes = (itemRequest.PersonalizacaoIds ?? []).Select(id => opcoes[id]).ToList();

                pedido.Itens.Add(new PedidoItens
                {
                    ProdutoId = variante.ProdutoId,
                    VarianteId = variante.Id,
                    Quantidade = itemRequest.Quantidade,
                    PrecoUnitario = variante.Produto.PrecoBase + personalizacoes.Sum(opcao => opcao.PrecoAdicional),
                    Customizacoes = personalizacoes.Select(opcao => new PedidoItemCustomizacoes
                    {
                        OpcaoId = opcao.Id,
                        Tipo = opcao.Tipo,
                        Valor = opcao.Valor,
                        PrecoAdicional = opcao.PrecoAdicional
                    }).ToList()
                });
            }

            pedido.Total = pedido.Itens.Sum(item => item.PrecoUnitario * item.Quantidade);

            if (pedido.Total > TOTAL_MAXIMO)
                throw new ErroEmValidacaoException(new List<string> { ResourceMensagensDeErro.PEDIDO_VALOR_MUITO_ALTO });

            await _pedidoRepository.Adicionar(pedido);

            await _salvarDB.Salvar();

            var pedidoSalvo = (await _pedidoRepository.ObterComDetalhes(pedido.Id))!;

            return MapearPedido.ParaResponse(pedidoSalvo);
        }

        private static void Validate(RequestCriarPedido request)
        {
            var validator = new CriarPedidoValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                // as regras de cada item se repetem por item: a mesma mensagem aparece uma vez só
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).Distinct().ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }

        // Peça que não existe, foi desativada ou saiu da vitrine, e personalização que não é deste produto
        private static void ValidarItens(RequestCriarPedido request, IDictionary<int, ProdutoVariantes> variantes, IDictionary<int, ProdutoOpcoes> opcoes)
        {
            var erros = new List<string>();

            foreach (var item in request.Itens)
            {
                if (variantes.TryGetValue(item.VarianteId, out var variante).IsFalse())
                {
                    erros.Add(ResourceMensagensDeErro.PECA_NAO_ENCONTRADA);
                    continue;
                }

                if (EstaNaVitrine(variante!).IsFalse())
                {
                    erros.Add(string.Format(ResourceMensagensDeErro.PECA_INDISPONIVEL, NomeDaPeca(variante!)));
                    continue;
                }

                foreach (var opcaoId in item.PersonalizacaoIds ?? [])
                {
                    var personalizacaoValida = opcoes.TryGetValue(opcaoId, out var opcao) &&
                        opcao!.ProdutoId == variante!.ProdutoId &&
                        opcao.Tipo == TipoOpcao.Personalizacao &&
                        opcao.Ativo;

                    if (personalizacaoValida.IsFalse())
                        erros.Add(string.Format(ResourceMensagensDeErro.PERSONALIZACAO_INVALIDA, variante!.Produto.Nome));
                }
            }

            if (erros.Count > 0)
                throw new ErroEmValidacaoException(erros.Distinct().ToList());
        }

        // A mesma variante pode aparecer em mais de um item (ex.: com e sem bordado): soma as quantidades
        private static void ValidarEstoque(RequestCriarPedido request, IDictionary<int, ProdutoVariantes> variantes)
        {
            var semEstoque = request.Itens
                .GroupBy(item => item.VarianteId)
                .Where(grupo => (variantes[grupo.Key].Estoque?.Quantidade ?? 0) < grupo.Sum(item => item.Quantidade))
                .Select(grupo => string.Format(ResourceMensagensDeErro.ESTOQUE_INSUFICIENTE_PECA, NomeDaPeca(variantes[grupo.Key])))
                .ToList();

            if (semEstoque.Count > 0)
                throw new ConflitoException(semEstoque);
        }

        // mesma regra da vitrine (ProdutoRepository.ObterAtivoComDetalhes)
        private static bool EstaNaVitrine(ProdutoVariantes variante) =>
            variante.Ativo &&
            variante.Produto.Ativo &&
            variante.TamanhoOpcao.Ativo &&
            (variante.CorOpcao is null || variante.CorOpcao.Ativo);

        // "Jaleco (M, Azul)" ou "Jaleco (M)"
        private static string NomeDaPeca(ProdutoVariantes variante) =>
            variante.CorOpcao is null
                ? $"{variante.Produto.Nome} ({variante.TamanhoOpcao.Valor})"
                : $"{variante.Produto.Nome} ({variante.TamanhoOpcao.Valor}, {variante.CorOpcao.Valor})";
    }
}

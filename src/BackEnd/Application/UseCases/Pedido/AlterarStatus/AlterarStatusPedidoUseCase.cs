using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Application.Services.MovimentarEstoque;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Pedido.AlterarStatus
{
    // O admin move o pedido pelo ciclo de vida (StatusPedidoExtension).
    // Pago baixa o estoque de cada peça, ligado ao pedido. Como só se cancela antes do pagamento,
    // o cancelamento nunca tem estoque para devolver.
    // Status, saldos e movimentações vão no mesmo SaveChanges: ou tudo é gravado, ou nada.
    public class AlterarStatusPedidoUseCase : IAlterarStatusPedidoUseCase
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IProdutoVarianteRepository _varianteRepository;
        private readonly IMovimentarEstoque _movimentarEstoque;
        private readonly ISalvarDBRepository _salvarDB;

        public AlterarStatusPedidoUseCase(IPedidoRepository pedidoRepository, IProdutoVarianteRepository varianteRepository, IMovimentarEstoque movimentarEstoque, ISalvarDBRepository salvarDB)
        {
            _pedidoRepository = pedidoRepository;
            _varianteRepository = varianteRepository;
            _movimentarEstoque = movimentarEstoque;
            _salvarDB = salvarDB;
        }

        public async Task<ResponsePedidoAdmin> Execute(int pedidoId, RequestAlterarStatusPedido request)
        {
            Validate(request);

            var novoStatus = Enum.Parse<StatusPedido>(request.Status.Trim(), ignoreCase: true);

            // rastreado desde o início: o xmin lido aqui é o conferido no SaveChanges, então se outro admin
            // alterar o pedido durante a operação, esta gravação inteira (status e estoque) é desfeita com 409
            var pedido = await _pedidoRepository.GetById(pedidoId)
                ?? throw new NaoEncontradoException(ResourceMensagensDeErro.PEDIDO_NAO_ENCONTRADO);

            if (pedido.Status.PodeMudarPara(novoStatus).IsFalse())
                throw new ConflitoException(string.Format(ResourceMensagensDeErro.TRANSICAO_STATUS_INVALIDA, pedido.Status, novoStatus));

            if (novoStatus == StatusPedido.Pago)
            {
                // peças agrupadas por variante (a mesma peça pode estar em itens com e sem personalização).
                // Itens antigos sem variante não têm estoque para movimentar.
                var pecas = pedido.Itens
                    .Where(item => item.VarianteId.HasValue)
                    .GroupBy(item => item.VarianteId!.Value)
                    .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(item => item.Quantidade));

                await ValidarEstoque(pecas);

                foreach (var (varianteId, quantidade) in pecas)
                    await _movimentarEstoque.Saida(varianteId, quantidade, $"Pedido #{pedido.Id} pago", pedido.Id);
            }

            // o UPDATE leva só status e atualizado_em
            pedido.Status = novoStatus;
            pedido.AtualizadoEm = DateTime.UtcNow;

            await _salvarDB.Salvar();

            var pedidoSalvo = (await _pedidoRepository.ObterComDetalhes(pedidoId))!;

            return MapearPedido.ParaResponseAdmin(pedidoSalvo);
        }

        private static void Validate(RequestAlterarStatusPedido request)
        {
            var validator = new AlterarStatusPedidoValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }

        // Confere todas as peças antes de mexer em qualquer uma, para listar tudo o que falta de uma vez
        private async Task ValidarEstoque(IDictionary<int, int> pecas)
        {
            var variantes = await _varianteRepository.ListarComEstoquePorIds(pecas.Keys.ToList());

            var semEstoque = variantes
                .Where(variante => (variante.Estoque?.Quantidade ?? 0) < pecas[variante.Id])
                .Select(variante => string.Format(ResourceMensagensDeErro.ESTOQUE_INSUFICIENTE_PECA, NomeDaPeca(variante)))
                .ToList();

            if (semEstoque.Count > 0)
                throw new ConflitoException(semEstoque);
        }

        // "Jaleco (M, Azul)" ou "Jaleco (M)"
        private static string NomeDaPeca(ProdutoVariantes variante) =>
            variante.CorOpcao is null
                ? $"{variante.Produto.Nome} ({variante.TamanhoOpcao.Valor})"
                : $"{variante.Produto.Nome} ({variante.TamanhoOpcao.Valor}, {variante.CorOpcao.Valor})";
    }
}

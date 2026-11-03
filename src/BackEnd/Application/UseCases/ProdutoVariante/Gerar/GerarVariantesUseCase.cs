using Application.Response;
using Domain.Entities;
using Domain.Enums;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoVariante.Gerar
{
    public class GerarVariantesUseCase : IGerarVariantesUseCase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoOpcaoRepository _opcaoRepository;
        private readonly IProdutoVarianteRepository _varianteRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public GerarVariantesUseCase(IProdutoRepository produtoRepository, IProdutoOpcaoRepository opcaoRepository, IProdutoVarianteRepository varianteRepository, ISalvarDBRepository salvarDB)
        {
            _produtoRepository = produtoRepository;
            _opcaoRepository = opcaoRepository;
            _varianteRepository = varianteRepository;
            _salvarDB = salvarDB;
        }

        public async Task<ResponseVariantesGeradas> Execute(int produtoId)
        {
            var produtoExiste = await _produtoRepository.Existe(produtoId);

            if (produtoExiste.IsFalse())
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            var opcoes = await _opcaoRepository.ListarAtivas(produtoId);
            var tamanhos = opcoes.Where(opcao => opcao.Tipo == TipoOpcao.Tamanho).ToList();
            var cores = opcoes.Where(opcao => opcao.Tipo == TipoOpcao.Cor).ToList();

            Validate(tamanhos);

            // produto sem cor: uma variante por tamanho, com cor nula
            var corIds = cores.Count > 0
                ? cores.Select(cor => (int?)cor.Id).ToList()
                : new List<int?> { null };

            var existentes = await _varianteRepository.ListarPorProduto(produtoId);
            var novas = new List<ProdutoVariantes>();

            foreach (var tamanho in tamanhos)
            {
                foreach (var corId in corIds)
                {
                    var jaExiste = existentes.Any(variante => variante.TamanhoOpcaoId == tamanho.Id && variante.CorOpcaoId == corId);

                    if (jaExiste)
                        continue;

                    novas.Add(new ProdutoVariantes
                    {
                        ProdutoId = produtoId,
                        TamanhoOpcaoId = tamanho.Id,
                        CorOpcaoId = corId,
                        Ativo = true,
                        Estoque = new Estoque { Quantidade = 0 }
                    });
                }
            }

            if (novas.Count > 0)
            {
                await _varianteRepository.Adicionar(novas);

                await _salvarDB.Salvar();
            }

            var valores = opcoes.ToDictionary(opcao => opcao.Id, opcao => opcao.Valor);

            return new ResponseVariantesGeradas
            {
                VariantesCriadas = novas.Select(variante => new ResponseProdutoVarianteAdmin
                {
                    Id = variante.Id,
                    TamanhoId = variante.TamanhoOpcaoId,
                    Tamanho = valores[variante.TamanhoOpcaoId],
                    CorId = variante.CorOpcaoId,
                    Cor = variante.CorOpcaoId.HasValue ? valores[variante.CorOpcaoId.Value] : null,
                    Ativo = variante.Ativo,
                    Quantidade = 0
                }).ToList()
            };
        }

        private static void Validate(IList<ProdutoOpcoes> tamanhos)
        {
            if (tamanhos.Count == 0)
                throw new ErroEmValidacaoException(new List<string> { ResourceMensagensDeErro.SEM_TAMANHO_ATIVO });
        }
    }
}

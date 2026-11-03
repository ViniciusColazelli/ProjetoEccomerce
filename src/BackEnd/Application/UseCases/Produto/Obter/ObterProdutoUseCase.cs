using Application.Response;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.Obter
{
    public class ObterProdutoUseCase : IObterProdutoUseCase
    {
        private readonly IProdutoRepository _produtoRepository;

        public ObterProdutoUseCase(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public async Task<ResponseProdutoDetalhe> Execute(int id)
        {
            var produto = await _produtoRepository.ObterAtivoComDetalhes(id);

            if (produto is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            var variantes = produto.Variantes
                .OrderBy(variante => variante.Id)
                .Select(variante => new ResponseProdutoVariante
                {
                    Id = variante.Id,
                    TamanhoId = variante.TamanhoOpcaoId,
                    CorId = variante.CorOpcaoId,
                    Disponivel = variante.Estoque?.Quantidade > 0
                }).ToList();

            return new ResponseProdutoDetalhe
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                PrecoBase = produto.PrecoBase,
                Disponivel = variantes.Any(variante => variante.Disponivel),
                Tamanhos = MapearOpcoes(produto, TipoOpcao.Tamanho),
                Cores = MapearOpcoes(produto, TipoOpcao.Cor),
                Personalizacoes = MapearOpcoes(produto, TipoOpcao.Personalizacao),
                Variantes = variantes
            };
        }

        private static IList<ResponseProdutoOpcao> MapearOpcoes(Produtos produto, TipoOpcao tipo)
        {
            return produto.Opcoes
                .Where(opcao => opcao.Tipo == tipo)
                .OrderBy(opcao => opcao.Id)
                .Select(opcao => new ResponseProdutoOpcao
                {
                    Id = opcao.Id,
                    Valor = opcao.Valor,
                    PrecoAdicional = opcao.PrecoAdicional
                }).ToList();
        }
    }
}

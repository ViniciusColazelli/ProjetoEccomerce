using Application.Response;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.ObterAdmin
{
    public class ObterProdutoAdminUseCase : IObterProdutoAdminUseCase
    {
        private readonly IProdutoRepository _produtoRepository;

        public ObterProdutoAdminUseCase(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public async Task<ResponseProdutoAdmin> Execute(int id)
        {
            var produto = await _produtoRepository.ObterComDetalhes(id);

            if (produto is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            return new ResponseProdutoAdmin
            {
                Id = produto.Id,
                Nome = produto.Nome,
                Descricao = produto.Descricao,
                PrecoBase = produto.PrecoBase,
                Ativo = produto.Ativo,
                Opcoes = produto.Opcoes
                    .OrderBy(opcao => opcao.Tipo)
                    .ThenBy(opcao => opcao.Id)
                    .Select(opcao => new ResponseProdutoOpcaoAdmin
                    {
                        Id = opcao.Id,
                        Tipo = opcao.Tipo,
                        Valor = opcao.Valor,
                        PrecoAdicional = opcao.PrecoAdicional,
                        Ativo = opcao.Ativo
                    }).ToList(),
                Variantes = produto.Variantes
                    .OrderBy(variante => variante.Id)
                    .Select(variante => new ResponseProdutoVarianteAdmin
                    {
                        Id = variante.Id,
                        TamanhoId = variante.TamanhoOpcaoId,
                        Tamanho = variante.TamanhoOpcao.Valor,
                        CorId = variante.CorOpcaoId,
                        Cor = variante.CorOpcao?.Valor,
                        Ativo = variante.Ativo,
                        Quantidade = variante.Estoque?.Quantidade ?? 0
                    }).ToList()
            };
        }
    }
}

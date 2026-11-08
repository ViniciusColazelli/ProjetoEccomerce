using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Services.Armazenamento;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoImagem.Ordenar
{
    // Recebe a lista completa das fotos na ordem nova e renumera (0, 1, 2...): a primeira vira a capa.
    // Exigir a lista completa evita ordem ambígua quando duas telas mexem ao mesmo tempo.
    public class OrdenarImagensUseCase : IOrdenarImagensUseCase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoImagemRepository _imagemRepository;
        private readonly ISalvarDBRepository _salvarDB;
        private readonly IArmazenamentoImagens _armazenamento;

        public OrdenarImagensUseCase(IProdutoRepository produtoRepository, IProdutoImagemRepository imagemRepository, ISalvarDBRepository salvarDB, IArmazenamentoImagens armazenamento)
        {
            _produtoRepository = produtoRepository;
            _imagemRepository = imagemRepository;
            _salvarDB = salvarDB;
            _armazenamento = armazenamento;
        }

        public async Task<IList<ResponseProdutoImagem>> Execute(int produtoId, RequestOrdenarImagens request)
        {
            var produtoExiste = await _produtoRepository.Existe(produtoId);

            if (produtoExiste.IsFalse())
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            var imagens = (await _imagemRepository.ListarPorProduto(produtoId)).ToDictionary(imagem => imagem.Id);
            var ids = request.Ids ?? [];

            // todas as fotos do produto, cada uma uma vez, e nenhuma de outro produto
            var listaCompleta = ids.Count == imagens.Count &&
                ids.Distinct().Count() == ids.Count &&
                ids.All(imagens.ContainsKey);

            if (listaCompleta.IsFalse())
                throw new ErroEmValidacaoException(new List<string> { ResourceMensagensDeErro.ORDEM_IMAGENS_INVALIDA });

            for (var posicao = 0; posicao < ids.Count; posicao++)
                imagens[ids[posicao]].Ordem = posicao;

            await _salvarDB.Salvar();

            return ids.Select(id => MapearImagem.ParaResponse(imagens[id], _armazenamento)).ToList();
        }
    }
}

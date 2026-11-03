using Application.Request;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.AlterarStatus
{
    public class AlterarStatusProdutoUseCase : IAlterarStatusProdutoUseCase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public AlterarStatusProdutoUseCase(IProdutoRepository produtoRepository, ISalvarDBRepository salvarDB)
        {
            _produtoRepository = produtoRepository;
            _salvarDB = salvarDB;
        }

        public async Task Execute(int id, RequestAlterarStatus request)
        {
            var produto = await _produtoRepository.GetById(id);

            if (produto is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            produto.Ativo = request.Ativo;

            _produtoRepository.Update(produto);

            await _salvarDB.Salvar();
        }
    }
}

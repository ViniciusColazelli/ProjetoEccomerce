using Application.Request;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.Update
{
    public class UpdateProdutoUseCase : IUpdateProdutoUseCase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public UpdateProdutoUseCase(IProdutoRepository produtoRepository, ISalvarDBRepository salvarDB)
        {
            _produtoRepository = produtoRepository;
            _salvarDB = salvarDB;
        }

        public async Task Execute(int id, RequestUpdateProduto request)
        {
            Validate(request);

            var produto = await _produtoRepository.GetById(id);

            if (produto is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.PRODUTO_NAO_ENCONTRADO);

            produto.Nome = request.Nome;
            produto.Descricao = request.Descricao;
            produto.PrecoBase = request.PrecoBase;

            _produtoRepository.Update(produto);

            await _salvarDB.Salvar();
        }

        private static void Validate(RequestUpdateProduto request)
        {
            var validator = new UpdateProdutoValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}

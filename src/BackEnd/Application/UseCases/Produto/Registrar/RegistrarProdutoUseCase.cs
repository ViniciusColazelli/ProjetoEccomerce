using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Produto.Registrar
{
    public class RegistrarProdutoUseCase : IRegistrarProdutoUseCase
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly ISalvarDBRepository _salvarDBRepository;

        public RegistrarProdutoUseCase(IProdutoRepository produtoRepository, ISalvarDBRepository salvarDBRepository)
        {
            _produtoRepository = produtoRepository;
            _salvarDBRepository = salvarDBRepository;
        }

        public async Task<ResponseProdutoRegistrado> Execute(RequestRegistrarProduto request)
        {
            Validate(request);

            var produto = MapearRequest.RequestParaEntidade(request);

            await _produtoRepository.Adicionar(produto);

            await _salvarDBRepository.Salvar();

            return new ResponseProdutoRegistrado
            {
                Id = produto.Id,
                Nome = produto.Nome
            };
        }

        private static void Validate(RequestRegistrarProduto request)
        {
            var validator = new RegistrarProdutoValidator();

            var result = validator.Validate(request);

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}

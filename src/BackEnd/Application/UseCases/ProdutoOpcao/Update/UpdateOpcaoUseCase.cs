using Application.Request;
using Domain.Entities;
using Domain.Extensions;
using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.ProdutoOpcao.Update
{
    public class UpdateOpcaoUseCase : IUpdateOpcaoUseCase
    {
        private readonly IProdutoOpcaoRepository _opcaoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public UpdateOpcaoUseCase(IProdutoOpcaoRepository opcaoRepository, ISalvarDBRepository salvarDB)
        {
            _opcaoRepository = opcaoRepository;
            _salvarDB = salvarDB;
        }

        public async Task Execute(int produtoId, int opcaoId, RequestUpdateOpcao request)
        {
            var opcao = await _opcaoRepository.GetById(produtoId, opcaoId);

            if (opcao is null)
                throw new NaoEncontradoException(ResourceMensagensDeErro.OPCAO_NAO_ENCONTRADA);

            await Validate(request, opcao);

            opcao.Valor = request.Valor;
            opcao.PrecoAdicional = request.PrecoAdicional;

            _opcaoRepository.Update(opcao);

            await _salvarDB.Salvar();
        }

        private async Task Validate(RequestUpdateOpcao request, ProdutoOpcoes opcao)
        {
            var validator = new UpdateOpcaoValidator();

            var result = validator.Validate(request);

            if (string.IsNullOrWhiteSpace(request.Valor).IsFalse())
            {
                var opcaoExiste = await _opcaoRepository.ExisteOpcao(opcao.ProdutoId, opcao.Tipo, request.Valor, ignorarOpcaoId: opcao.Id);
                if (opcaoExiste)
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure(string.Empty, ResourceMensagensDeErro.OPCAO_JA_CADASTRADA));
            }

            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}

using Application.Request;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Services.ClienteLogado;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Update
{
    public class UpdateClienteUseCase : IUpdateClienteUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        private readonly ISalvarDBRepository _salvarDB;
        private readonly IClienteRepository _clienteRepository;

        public UpdateClienteUseCase(IClienteLogado clienteLogado, ISalvarDBRepository salvarDB, IClienteRepository clienteRepository)
        {
            _clienteLogado = clienteLogado;
            _salvarDB = salvarDB;
            _clienteRepository = clienteRepository;
        }
        public async Task Execute(RequestUpdateCliente request)
        {
            var clienteLogado = await _clienteLogado.Cliente();

            await Validate(request, clienteLogado.Email);

            var user = await _clienteRepository.GetById(clienteLogado.Id);

            user.Nome = request.Nome;
            user.Email = request.Email;

            _clienteRepository.Update(user);

            await _salvarDB.Salvar();
        }
        private async Task Validate(RequestUpdateCliente request, string currentEmail)
        {
            var validator = new UpdateClienteValidator();

            var result = validator.Validate(request);

            if (currentEmail.Equals(request.Email).IsFalse()) // esse isFalse é um metodo para indentificar se é falso no caso é o que a gente quer pra entrar no if.
            {
                var userExist = await _clienteRepository.ExisteClienteComEmail(request.Email);
                if (userExist)
                    result.Errors.Add(new FluentValidation.Results.ValidationFailure("email", ResourceMensagensDeErro.EMAIL_JA_REGISTRADO));
            }
            if (result.IsValid.IsFalse())
            {
                var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(errorMessages);
            }
        }
    }
}

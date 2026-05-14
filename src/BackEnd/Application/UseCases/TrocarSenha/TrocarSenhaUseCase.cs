using Application.Request;
using Domain.Entities;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Security.Criptografia;
using Domain.Services.ClienteLogado;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.TrocarSenha
{
    public class TrocarSenhaUseCase : ITrocarSenhaUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        private readonly IClienteRepository _repository;
        private readonly ISenhaCriptografada _senhaCriptografada;
        private readonly ISalvarDBRepository _salvarDB;

        public TrocarSenhaUseCase(IClienteLogado clienteLogado, IClienteRepository repository, ISenhaCriptografada senhaCriptografada, ISalvarDBRepository salvarDB)
        {
            _clienteLogado = clienteLogado;
            _repository = repository;
            _senhaCriptografada = senhaCriptografada;
            _salvarDB = salvarDB;
        }

        public async Task Execute(RequestTrocarSenha request)
        {
            var clienteLogado = await _clienteLogado.Cliente();

            Validate(request, clienteLogado);

            var user = await _repository.GetById(clienteLogado.Id);

            user.Senha = _senhaCriptografada.Criptografia(request.NovaSenha);

            _repository.Update(user);

            await _salvarDB.Salvar();
        }

        private void Validate(RequestTrocarSenha request, Clientes clienteLogado)
        {
            var result = new TrocarSenhaValidator().Validate(request);

            var currentPasswordEncripted = _senhaCriptografada.Criptografia(request.Senha);

            if (currentPasswordEncripted.Equals(clienteLogado.Senha).IsFalse())
                result.Errors.Add(new FluentValidation.Results.ValidationFailure(string.Empty, ResourceMensagensDeErro.SENHA_ATUAL_NAO_REGISTRADA));

            if (result.IsValid.IsFalse())
                throw new ErroEmValidacaoException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}


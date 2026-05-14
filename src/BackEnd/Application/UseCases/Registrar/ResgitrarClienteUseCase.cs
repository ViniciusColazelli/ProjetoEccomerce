using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Repositories;
using Domain.Security.Criptografia;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Registrar
{
    public class ResgitrarClienteUseCase : IRegistrarClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;
        private readonly ISenhaCriptografada _senhaCriptografada;
        private readonly ISalvarDBRepository _salvarDBRepository;

        public ResgitrarClienteUseCase(IClienteRepository clienteRepository, ISenhaCriptografada senhaCriptografada, ISalvarDBRepository salvarDBRepository)
        {
            _clienteRepository = clienteRepository;
            _senhaCriptografada = senhaCriptografada;
            _salvarDBRepository = salvarDBRepository;
        }

        public async Task<ResponseClienteRegistrado> Execute(RequestRegistrarCliente request)
        {
            await ValidarRequest(request);

            var cliente = MapearRequest.RequestParaEntidade(request);

            cliente.Senha = _senhaCriptografada.Criptografia(request.Senha);

            //Salvar no DB
            await _clienteRepository.Adicionar(cliente);

            await _salvarDBRepository.Salvar();

            return new ResponseClienteRegistrado
            {
                Nome = request.Nome
            };
        }

        private async Task ValidarRequest(RequestRegistrarCliente request)
        {
            var validator = new RegistrarClienteValidator();

            var result = validator.Validate(request);

            var emailExist = await _clienteRepository.ExisteClienteComEmail(request.Email);
            if (emailExist)
            {
                result.Errors.Add(new FluentValidation.Results.ValidationFailure(string.Empty, ResourceMensagensDeErro.EMAIL_JA_REGISTRADO));
            }

            if (result.IsValid == false)
            {
                var mensagemErro = result.Errors.Select(e => e.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(mensagemErro);
            }
        }
    }
}

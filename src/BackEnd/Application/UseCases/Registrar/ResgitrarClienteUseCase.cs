using Application.Criptografia;
using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Domain.Repositories.Cliente;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Registrar
{
    public class ResgitrarClienteUseCase
    {
        private readonly IClienteRepository _clienteRepository;


        public async Task<ResponseClienteRegistrado> Execute(RequestRegistrarCliente request)
        {
            var criptografiaDeSenha = new CriptografiaDeSenha();

            ValidarRequest(request);

            var cliente = MapearRequest.RequestParaEntidade(request);

            cliente.Senha = criptografiaDeSenha.Criptografia(cliente.Senha);

            //Salvar no BD
            await _clienteRepository.Adicionar(cliente);

            return new ResponseClienteRegistrado
            {
                Nome = request.Nome
            };
        }

        private void ValidarRequest(RequestRegistrarCliente request)
        {
            var validator = new RegistrarClienteValidator();

            var result = validator.Validate(request);

            if (result.IsValid == false)
            {
                var mensagemErro = result.Errors.Select(e => e.ErrorMessage).ToList();

                throw new ErroEmValidacaoException(mensagemErro);
            }
        }
    }
}

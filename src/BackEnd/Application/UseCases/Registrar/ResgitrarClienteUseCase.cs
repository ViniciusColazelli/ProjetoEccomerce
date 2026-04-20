using Application.Criptografia;
using Application.Request;
using Application.Response;
using Application.Services.Mapeamento;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Registrar
{
    public class ResgitrarClienteUseCase
    {
        public ResponseClienteRegistrado Execute(RequestRegistrarCliente request)
        {
            var criptografiaDeSenha = new CriptografiaDeSenha();

            ValidarRequest(request);

            var cliente = MapearRequest.RequestParaEntidade(request);

            cliente.Senha = criptografiaDeSenha.Criptografia(cliente.Senha);
            //Salvar no BD

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

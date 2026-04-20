using Application.Request;
using Application.Response;
using Exceptions.ExceptionBase;
using System.Text.Json;

namespace Application.UseCases.Registrar
{
    public class ResgitrarClienteUseCase
    {
        public ResponseClienteRegistrado Execute(RequestRegistrarCliente request)
        {
            
            ValidarRequest(request);

            //Mapear nossa Request para a entidade
            //Fazer a criptografia da senha
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

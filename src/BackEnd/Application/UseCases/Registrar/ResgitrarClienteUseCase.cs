using Application.Request;
using Application.Response;

namespace Application.UseCases.Registrar
{
    public class ResgitrarClienteUseCase
    {
        public ResponseClienteRegistrado Execute(RequestRegistrarCliente request)
        {
        //Validar nossa Request
        //Mapear nossa Request para a entidade
        //Fazer a criptografia da senha
        //Salvar no BD

            return new ResponseClienteRegistrado
            {
                Nome = request.Nome
            };
        }
    }
}

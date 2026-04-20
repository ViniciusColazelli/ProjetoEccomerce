using Application.Request;
using Domain.Entities;

namespace Application.Services.Mapeamento
{
    public static class MapearRequest
    {
        public static Clientes RequestParaEntidade(RequestRegistrarCliente request)
        {
            return new Clientes
            {
                Nome = request.Nome,
                Email = request.Email
            };
        }
    }
}

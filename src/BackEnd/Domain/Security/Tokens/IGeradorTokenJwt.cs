using Domain.Entities;

namespace Domain.Security.Tokens
{
    public interface IGeradorTokenJwt
    {
        string GerarToken(Clientes cliente);
    }
}
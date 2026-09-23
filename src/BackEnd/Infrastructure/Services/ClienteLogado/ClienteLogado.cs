using Domain.Entities;
using Domain.Services.ClienteLogado;
using Exceptions.ExceptionBase;
using Infrastructure.DataAcess;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.ClienteLogado
{
    public class ClienteLogado : IClienteLogado
    {
        private readonly EccomerceDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClienteLogado(EccomerceDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<Clientes> Cliente()
        {
            var clienteId = _httpContextAccessor.HttpContext!.Session.GetInt32("ClienteId");

            if (clienteId is null)
            {
                throw new ClienteNaoLogadoException();
            }

            return await _dbContext.Clientes.AsNoTracking().FirstAsync(user => user.Id == clienteId);
        }
    }
}

using System.Security.Claims;
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
            var clienteIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);

            if (clienteIdClaim is null || int.TryParse(clienteIdClaim.Value, out var clienteId) is false)
            {
                throw new ClienteNaoLogadoException();
            }

            return await _dbContext.Clientes.AsNoTracking().FirstAsync(cliente => cliente.Id == clienteId);
        }
    }
}
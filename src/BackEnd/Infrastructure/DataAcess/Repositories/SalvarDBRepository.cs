using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAcess.Repositories
{
    public class SalvarDBRepository : ISalvarDBRepository
    {
        private readonly EccomerceDbContext _dbContext;

        public SalvarDBRepository(EccomerceDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task Salvar()
        {
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflitoException(ResourceMensagensDeErro.REGISTRO_ALTERADO_POR_OUTRA_OPERACAO);
            }
        }
    }
}

using Domain.Repositories;
using Exceptions;
using Exceptions.ExceptionBase;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            // duas requisições inserindo a mesma chave ao mesmo tempo (ex.: a primeira entrada de uma variante sem estoque):
            // a segunda perde a corrida e recebe 409, como no conflito de atualização
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                throw new ConflitoException(ResourceMensagensDeErro.REGISTRO_ALTERADO_POR_OUTRA_OPERACAO);
            }
        }
    }
}

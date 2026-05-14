using Application.Response;

namespace Application.UseCases.Profile
{
    public interface IGetClienteProfileUseCase
    {
        public Task<ResponseClienteProfile> Execute();
    }
}

using Application.Request;

namespace Application.UseCases.TrocarSenha
{
    public interface ITrocarSenhaUseCase
    {
        public Task Execute(RequestTrocarSenha request);
    }
}

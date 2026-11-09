using Application.Request;
using Application.Response;

namespace Application.UseCases.TrocarSenha
{
    public interface ITrocarSenhaUseCase
    {
        // devolve a sessão nova de quem trocou a senha (as outras são revogadas)
        public Task<TokensSessao> Execute(RequestTrocarSenha request);
    }
}

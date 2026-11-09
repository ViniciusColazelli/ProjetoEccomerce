using Application.Response;

namespace Application.UseCases.Sessao.Renovar
{
    public interface IRenovarSessaoUseCase
    {
        // refreshToken: o valor do cookie (null quando o cookie não veio)
        public Task<TokensSessao> Execute(string? refreshToken);
    }
}

namespace Application.UseCases.Sessao.Encerrar
{
    public interface IEncerrarSessaoUseCase
    {
        // refreshToken: o valor do cookie (null quando o cookie não veio)
        public Task Execute(string? refreshToken);
    }
}

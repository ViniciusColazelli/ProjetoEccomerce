using Application.Request;
using Application.Response;
using Domain.Repositories;
using Domain.Security.Criptografia;
using Domain.Security.Tokens;
using Exceptions.ExceptionBase;

namespace Application.UseCases.Login
{
    public class LoginClienteUseCase : ILoginClienteUseCase
    {
        private readonly IClienteRepository _repository;
        private readonly ISenhaCriptografada _senhaCriptografada;
        private readonly IGeradorTokenJwt _geradorTokenJwt;

        public LoginClienteUseCase(IClienteRepository repository, ISenhaCriptografada senhaCriptografada, IGeradorTokenJwt geradorTokenJwt)
        {
            _repository = repository;
            _senhaCriptografada = senhaCriptografada;
            _geradorTokenJwt = geradorTokenJwt;
        }

        public async Task<ResponseClienteLogado> Execute(RequestLoginCliente request)
        {
            var senhaCriptografada = _senhaCriptografada.Criptografia(request.Senha);

            var user = await _repository.GetEmailAndPassword(request.Email, senhaCriptografada) ?? throw new ErroEmLoginException();

            var token = _geradorTokenJwt.GerarToken(user);

            return new ResponseClienteLogado()
            {
                Nome = user.Nome,
                Token = token
            };
        }
    }
}
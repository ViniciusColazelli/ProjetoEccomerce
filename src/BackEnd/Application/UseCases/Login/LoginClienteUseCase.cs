using Application.Request;
using Application.Response;
using Domain.Extensions;
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
        private readonly ISalvarDBRepository _salvarDB;

        public LoginClienteUseCase(IClienteRepository repository, ISenhaCriptografada senhaCriptografada, IGeradorTokenJwt geradorTokenJwt, ISalvarDBRepository salvarDB)
        {
            _repository = repository;
            _senhaCriptografada = senhaCriptografada;
            _geradorTokenJwt = geradorTokenJwt;
            _salvarDB = salvarDB;
        }

        public async Task<ResponseClienteLogado> Execute(RequestLoginCliente request)
        {
            // o hash tem sal aleatório (BCrypt): não dá para buscar por ele, então busca pelo e-mail e verifica a senha.
            // Sem conta, a verificação roda mesmo assim (contra um hash fictício): a resposta leva o mesmo tempo
            var user = await _repository.GetByEmail(request.Email);

            var senhaConfere = _senhaCriptografada.Verificar(request.Senha, user?.Senha);

            if (user is null || senhaConfere.IsFalse())
                throw new ErroEmLoginException();

            // conta ainda no formato antigo (SHA-512): com a senha confirmada, regrava em BCrypt
            if (_senhaCriptografada.PrecisaAtualizar(user.Senha))
            {
                user.Senha = _senhaCriptografada.Gerar(request.Senha);

                await _salvarDB.Salvar();
            }

            var token = _geradorTokenJwt.GerarToken(user);

            return new ResponseClienteLogado()
            {
                Nome = user.Nome,
                Token = token
            };
        }
    }
}

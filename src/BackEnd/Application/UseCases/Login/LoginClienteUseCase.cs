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
        private readonly IGeradorRefreshToken _geradorRefreshToken;
        private readonly ISessaoRepository _sessaoRepository;
        private readonly ISalvarDBRepository _salvarDB;

        public LoginClienteUseCase(IClienteRepository repository, ISenhaCriptografada senhaCriptografada, IGeradorTokenJwt geradorTokenJwt,
            IGeradorRefreshToken geradorRefreshToken, ISessaoRepository sessaoRepository, ISalvarDBRepository salvarDB)
        {
            _repository = repository;
            _senhaCriptografada = senhaCriptografada;
            _geradorTokenJwt = geradorTokenJwt;
            _geradorRefreshToken = geradorRefreshToken;
            _sessaoRepository = sessaoRepository;
            _salvarDB = salvarDB;
        }

        public async Task<ResultadoLogin> Execute(RequestLoginCliente request)
        {
            // o hash tem sal aleatório (BCrypt): não dá para buscar por ele, então busca pelo e-mail e verifica a senha.
            // Sem conta, a verificação roda mesmo assim (contra um hash fictício): a resposta leva o mesmo tempo
            var user = await _repository.GetByEmail(request.Email);

            var senhaConfere = _senhaCriptografada.Verificar(request.Senha, user?.Senha);

            if (user is null || senhaConfere.IsFalse())
                throw new ErroEmLoginException();

            // conta ainda no formato antigo (SHA-512): com a senha confirmada, regrava em BCrypt
            if (_senhaCriptografada.PrecisaAtualizar(user.Senha))
                user.Senha = _senhaCriptografada.Gerar(request.Senha);

            // cada login é uma sessão nova (outro aparelho ou navegador): as outras continuam valendo
            var refresh = _geradorRefreshToken.Criar(user.Id);
            await _sessaoRepository.Adicionar(refresh.Sessao);

            await _salvarDB.Salvar();

            var tokens = new TokensSessao(_geradorTokenJwt.GerarToken(user), refresh.Token, refresh.Sessao.ExpiraEm);

            // o token não trafega no corpo da resposta: vai só nos cookies
            return new ResultadoLogin(new ResponseClienteLogado { Nome = user.Nome }, tokens);
        }
    }
}

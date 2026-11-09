using Application.Request;
using Application.Response;
using Domain.Entities;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Security.Criptografia;
using Domain.Security.Tokens;
using Domain.Services.ClienteLogado;
using Exceptions;
using Exceptions.ExceptionBase;

namespace Application.UseCases.TrocarSenha
{
    public class TrocarSenhaUseCase : ITrocarSenhaUseCase
    {
        private readonly IClienteLogado _clienteLogado;
        private readonly IClienteRepository _repository;
        private readonly ISenhaCriptografada _senhaCriptografada;
        private readonly ISessaoRepository _sessaoRepository;
        private readonly IGeradorRefreshToken _geradorRefreshToken;
        private readonly IGeradorTokenJwt _geradorTokenJwt;
        private readonly ISalvarDBRepository _salvarDB;

        public TrocarSenhaUseCase(IClienteLogado clienteLogado, IClienteRepository repository, ISenhaCriptografada senhaCriptografada,
            ISessaoRepository sessaoRepository, IGeradorRefreshToken geradorRefreshToken, IGeradorTokenJwt geradorTokenJwt, ISalvarDBRepository salvarDB)
        {
            _clienteLogado = clienteLogado;
            _repository = repository;
            _senhaCriptografada = senhaCriptografada;
            _sessaoRepository = sessaoRepository;
            _geradorRefreshToken = geradorRefreshToken;
            _geradorTokenJwt = geradorTokenJwt;
            _salvarDB = salvarDB;
        }

        public async Task<TokensSessao> Execute(RequestTrocarSenha request)
        {
            var clienteLogado = await _clienteLogado.Cliente();

            Validate(request, clienteLogado);

            var user = await _repository.GetById(clienteLogado.Id);

            user.Senha = _senhaCriptografada.Gerar(request.NovaSenha);

            _repository.Update(user);

            // Quem tinha a senha antiga (ou uma cópia do cookie) perde o acesso: todas as sessões caem,
            // inclusive a atual, e quem trocou a senha ganha uma nova e continua logado
            var agora = DateTime.UtcNow;

            foreach (var sessao in await _sessaoRepository.ListarAtivas(user.Id, agora))
                sessao.RevogadoEm = agora;

            var refresh = _geradorRefreshToken.Criar(user.Id);
            await _sessaoRepository.Adicionar(refresh.Sessao);

            await _salvarDB.Salvar();

            return new TokensSessao(_geradorTokenJwt.GerarToken(user), refresh.Token, refresh.Sessao.ExpiraEm);
        }

        private void Validate(RequestTrocarSenha request, Clientes clienteLogado)
        {
            var result = new TrocarSenhaValidator().Validate(request);

            // aceita a senha atual em BCrypt ou no formato antigo; a nova é sempre gravada em BCrypt
            if (_senhaCriptografada.Verificar(request.Senha, clienteLogado.Senha).IsFalse())
                result.Errors.Add(new FluentValidation.Results.ValidationFailure(string.Empty, ResourceMensagensDeErro.SENHA_ATUAL_NAO_REGISTRADA));

            if (result.IsValid.IsFalse())
                throw new ErroEmValidacaoException(result.Errors.Select(e => e.ErrorMessage).ToList());
        }
    }
}

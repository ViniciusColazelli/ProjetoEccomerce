using Application.Request;
using Application.Response;
using Domain.Repositories;
using Domain.Security.Criptografia;
using Exceptions.ExceptionBase;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Login
{
    public class LoginClienteUseCase : ILoginClienteUseCase
    {
        private readonly IClienteRepository _repository;
        private readonly ISenhaCriptografada _senhaCriptografada;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginClienteUseCase(IClienteRepository repository, ISenhaCriptografada senhaCriptografada, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _senhaCriptografada = senhaCriptografada;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<ResponseClienteRegistrado> Execute(RequestLoginCliente request)
        {
            var senhaCriptografada = _senhaCriptografada.Criptografia(request.Senha);

            var user = await _repository.GetEmailAndPassword(request.Email, senhaCriptografada) ?? throw new ErroEmLoginException();
                                                                                                                                        
            _httpContextAccessor.HttpContext!.Session.SetInt32("ClienteId", user.Id);   
            
            return new ResponseClienteRegistrado()
            {
                Nome = user.Nome
            };
        }
    }
}

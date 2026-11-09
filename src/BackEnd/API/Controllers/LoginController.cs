using API.Extensions;
using Application.Request;
using Application.Response;
using Application.UseCases.Login;
using Application.UseCases.Sessao.Encerrar;
using Application.UseCases.Sessao.Renovar;
using Infrastructure.Security.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitExtension.PoliticaAutenticacao)]
        [ProducesResponseType(typeof(ResponseClienteLogado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromServices] ILoginClienteUseCase useCase, [FromBody] RequestLoginCliente request)
        {
            var result = await useCase.Execute(request);

            Response.EscreverCookiesDaSessao(result.Tokens, _configuration);

            return Ok(result.Resposta);
        }

        // Troca o cookie refresh_token por cookies novos (JWT e refresh token).
        // Se falhar, não mexe nos cookies: outra aba pode ter acabado de gravar os novos
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitExtension.PoliticaRenovacao)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Renovar([FromServices] IRenovarSessaoUseCase useCase)
        {
            Request.Cookies.TryGetValue(CookieAutenticacao.NomeRefresh, out var refreshToken);

            var tokens = await useCase.Execute(refreshToken);

            Response.EscreverCookiesDaSessao(tokens, _configuration);

            return NoContent();
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromServices] IEncerrarSessaoUseCase useCase)
        {
            Request.Cookies.TryGetValue(CookieAutenticacao.NomeRefresh, out var refreshToken);

            await useCase.Execute(refreshToken);

            Response.RemoverCookiesDaSessao();

            return NoContent();
        }
    }
}

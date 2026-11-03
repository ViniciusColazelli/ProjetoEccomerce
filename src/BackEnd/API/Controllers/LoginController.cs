using API.Extensions;
using Application.Request;
using Application.Response;
using Application.UseCases.Login;
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
        [ProducesResponseType(typeof(ResponseClienteRegistrado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromServices] ILoginClienteUseCase useCase, [FromBody] RequestLoginCliente request)
        {
            var result = await useCase.Execute(request);

            Response.EscreverTokenNoCookie(result.Token, _configuration);
            result.Token = string.Empty;   // o token não trafega mais no corpo da resposta

            return Ok(result);
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            Response.RemoverTokenDoCookie();

            return NoContent();
        }
    }
}
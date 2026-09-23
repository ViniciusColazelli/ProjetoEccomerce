using Application.Request;
using Application.Response;
using Application.UseCases.Login;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(ResponseClienteRegistrado), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromServices] ILoginClienteUseCase useCase, [FromBody] RequestLoginCliente request)
        {
            var result = await useCase.Execute(request);

            return Ok(result);
        }
    }
}

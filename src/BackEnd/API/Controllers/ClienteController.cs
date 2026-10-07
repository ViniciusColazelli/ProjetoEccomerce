using Application.Request;
using Application.Response;
using Application.UseCases.Profile;
using Application.UseCases.Registrar;
using Application.UseCases.TrocarSenha;
using Application.UseCases.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResponseClienteRegistrado), StatusCodes.Status201Created)]
        public async Task<IActionResult> Registrar([FromServices] IRegistrarClienteUseCase useCase, [FromBody]RequestRegistrarCliente request)
        {
            var result = await useCase.Execute(request);

            return Created(string.Empty, result);
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ResponseClienteProfile), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserProfile([FromServices] IGetClienteProfileUseCase useCase)
        {
            var result = await useCase.Execute();

            return Ok(result);
        }

        [HttpPut]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Atualizar([FromServices] IUpdateClienteUseCase useCase, [FromBody] RequestUpdateCliente request)
        {
            await useCase.Execute(request);

            return NoContent();
        }

        [HttpPut("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AlterarSenha([FromServices] ITrocarSenhaUseCase useCase, [FromBody] RequestTrocarSenha request)
        {
            await useCase.Execute(request);

            return NoContent();
        }
    }
}

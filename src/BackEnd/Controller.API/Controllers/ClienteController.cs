using Application.Request;
using Application.Response;
using Application.UseCases.Registrar;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(ResponseClienteRegistrado), StatusCodes.Status201Created)]
        public async Task<IActionResult> Registrar([FromServices] IRegistrarClienteUseCase useCase, [FromBody]RequestRegistrarCliente request)
        {
            var result = await useCase.Execute(request);

            return Created(string.Empty, result);
        }
    }
}

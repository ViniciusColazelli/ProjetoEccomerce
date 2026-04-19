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
        public IActionResult Registrar(RequestRegistrarCliente request)
        {
            var useCase = new ResgitrarClienteUseCase(); 

            var result = useCase.Execute(request);

            return Created(string.Empty, result);
        }
    }
}

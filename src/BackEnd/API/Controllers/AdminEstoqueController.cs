using Application.Request;
using Application.Response;
using Application.UseCases.EstoqueVariante.Listar;
using Application.UseCases.EstoqueVariante.Obter;
using Domain.Security.Perfis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/admin/estoque")]
    [ApiController]
    [Authorize(Roles = Perfis.Admin)]
    public class AdminEstoqueController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ResponseEstoque), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar([FromServices] IListarEstoqueUseCase useCase, [FromQuery] RequestListarEstoque request)
        {
            var result = await useCase.Execute(request);

            return Ok(result);
        }

        [HttpGet("{varianteId:int}")]
        [ProducesResponseType(typeof(ResponseEstoqueVariante), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obter([FromServices] IObterEstoqueUseCase useCase, [FromRoute] int varianteId)
        {
            var result = await useCase.Execute(varianteId);

            return Ok(result);
        }
    }
}

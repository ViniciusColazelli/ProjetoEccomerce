using Application.Request;
using Application.Response;
using Application.UseCases.EstoqueVariante.Ajustar;
using Application.UseCases.EstoqueVariante.Entrada;
using Application.UseCases.EstoqueVariante.Historico;
using Application.UseCases.EstoqueVariante.Listar;
using Application.UseCases.EstoqueVariante.Obter;
using Application.UseCases.EstoqueVariante.Saida;
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

        [HttpGet("movimentacoes")]
        [ProducesResponseType(typeof(ResponseMovimentacoes), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ListarMovimentacoes([FromServices] IListarMovimentacoesUseCase useCase, [FromQuery] RequestListarMovimentacoes request)
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

        [HttpPost("{varianteId:int}/entrada")]
        [ProducesResponseType(typeof(ResponseEstoqueVariante), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Entrada([FromServices] IEntradaEstoqueUseCase useCase, [FromRoute] int varianteId, [FromBody] RequestMovimentarEstoque request)
        {
            var result = await useCase.Execute(varianteId, request);

            return Ok(result);
        }

        [HttpPost("{varianteId:int}/saida")]
        [ProducesResponseType(typeof(ResponseEstoqueVariante), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Saida([FromServices] ISaidaEstoqueUseCase useCase, [FromRoute] int varianteId, [FromBody] RequestMovimentarEstoque request)
        {
            var result = await useCase.Execute(varianteId, request);

            return Ok(result);
        }

        [HttpPost("{varianteId:int}/ajuste")]
        [ProducesResponseType(typeof(ResponseEstoqueVariante), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Ajustar([FromServices] IAjustarEstoqueUseCase useCase, [FromRoute] int varianteId, [FromBody] RequestAjustarEstoque request)
        {
            var result = await useCase.Execute(varianteId, request);

            return Ok(result);
        }
    }
}

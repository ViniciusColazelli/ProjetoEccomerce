using Application.Request;
using Application.Response;
using Application.UseCases.Pedido.Cancelar;
using Application.UseCases.Pedido.Criar;
using Application.UseCases.Pedido.ListarMeus;
using Application.UseCases.Pedido.ObterMeu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Pedidos do cliente logado: cada rota só enxerga os pedidos dele
    [Route("api/pedidos")]
    [ApiController]
    [Authorize]
    [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
    public class PedidoController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ResponsePedidos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar([FromServices] IListarMeusPedidosUseCase useCase, [FromQuery] RequestListarMeusPedidos request)
        {
            var result = await useCase.Execute(request);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResponsePedido), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obter([FromServices] IObterMeuPedidoUseCase useCase, [FromRoute] int id)
        {
            var result = await useCase.Execute(id);

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ResponsePedido), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Criar([FromServices] ICriarPedidoUseCase useCase, [FromBody] RequestCriarPedido request)
        {
            var result = await useCase.Execute(request);

            return Created($"/api/pedidos/{result.Id}", result);
        }

        [HttpPost("{id:int}/cancelar")]
        [ProducesResponseType(typeof(ResponsePedido), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Cancelar([FromServices] ICancelarMeuPedidoUseCase useCase, [FromRoute] int id)
        {
            var result = await useCase.Execute(id);

            return Ok(result);
        }
    }
}

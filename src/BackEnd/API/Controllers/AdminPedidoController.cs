using Application.Request;
using Application.Response;
using Application.UseCases.Pedido.AlterarStatus;
using Application.UseCases.Pedido.ListarAdmin;
using Application.UseCases.Pedido.ObterAdmin;
using Domain.Security.Perfis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/admin/pedidos")]
    [ApiController]
    [Authorize(Roles = Perfis.Admin)]
    [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status403Forbidden)]
    public class AdminPedidoController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ResponsePedidosAdmin), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar([FromServices] IListarPedidosAdminUseCase useCase, [FromQuery] RequestListarPedidosAdmin request)
        {
            var result = await useCase.Execute(request);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResponsePedidoAdmin), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obter([FromServices] IObterPedidoAdminUseCase useCase, [FromRoute] int id)
        {
            var result = await useCase.Execute(id);

            return Ok(result);
        }

        // Pago baixa o estoque; cancelar só é possível antes do pagamento
        [HttpPost("{id:int}/status")]
        [ProducesResponseType(typeof(ResponsePedidoAdmin), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AlterarStatus([FromServices] IAlterarStatusPedidoUseCase useCase, [FromRoute] int id, [FromBody] RequestAlterarStatusPedido request)
        {
            var result = await useCase.Execute(id, request);

            return Ok(result);
        }
    }
}

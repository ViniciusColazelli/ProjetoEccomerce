using Application.Request;
using Application.Response;
using Application.UseCases.Produto.Listar;
using Application.UseCases.Produto.Obter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProdutoController : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResponseProdutos), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar([FromServices] IListarProdutosUseCase useCase, [FromQuery] RequestListarProdutos request)
        {
            var result = await useCase.Execute(request);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResponseProdutoDetalhe), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obter([FromServices] IObterProdutoUseCase useCase, [FromRoute] int id)
        {
            var result = await useCase.Execute(id);

            return Ok(result);
        }
    }
}

using Application.Request;
using Application.Response;
using Application.UseCases.Produto.AlterarStatus;
using Application.UseCases.Produto.ListarAdmin;
using Application.UseCases.Produto.ObterAdmin;
using Application.UseCases.Produto.Registrar;
using Application.UseCases.Produto.Update;
using Domain.Security.Perfis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/admin/produto")]
    [ApiController]
    [Authorize(Roles = Perfis.Admin)]
    public class AdminProdutoController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(ResponseProdutosAdmin), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Listar([FromServices] IListarProdutosAdminUseCase useCase, [FromQuery] RequestListarProdutosAdmin request)
        {
            var result = await useCase.Execute(request);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResponseProdutoAdmin), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Obter([FromServices] IObterProdutoAdminUseCase useCase, [FromRoute] int id)
        {
            var result = await useCase.Execute(id);

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ResponseProdutoRegistrado), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Registrar([FromServices] IRegistrarProdutoUseCase useCase, [FromBody] RequestRegistrarProduto request)
        {
            var result = await useCase.Execute(request);

            return Created(string.Empty, result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar([FromServices] IUpdateProdutoUseCase useCase, [FromRoute] int id, [FromBody] RequestUpdateProduto request)
        {
            await useCase.Execute(id, request);

            return NoContent();
        }

        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AlterarStatus([FromServices] IAlterarStatusProdutoUseCase useCase, [FromRoute] int id, [FromBody] RequestAlterarStatus request)
        {
            await useCase.Execute(id, request);

            return NoContent();
        }
    }
}

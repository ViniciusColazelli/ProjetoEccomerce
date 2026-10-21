using Application.Request;
using Application.Response;
using Application.UseCases.ProdutoOpcao.AlterarStatus;
using Application.UseCases.ProdutoOpcao.Registrar;
using Application.UseCases.ProdutoOpcao.Remover;
using Application.UseCases.ProdutoOpcao.Update;
using Domain.Security.Perfis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/admin/produto/{produtoId:int}/opcao")]
    [ApiController]
    [Authorize(Roles = Perfis.Admin)]
    public class AdminProdutoOpcaoController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(ResponseProdutoOpcaoAdmin), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Registrar([FromServices] IRegistrarOpcaoUseCase useCase, [FromRoute] int produtoId, [FromBody] RequestRegistrarOpcao request)
        {
            var result = await useCase.Execute(produtoId, request);

            return Created(string.Empty, result);
        }

        [HttpPut("{opcaoId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar([FromServices] IUpdateOpcaoUseCase useCase, [FromRoute] int produtoId, [FromRoute] int opcaoId, [FromBody] RequestUpdateOpcao request)
        {
            await useCase.Execute(produtoId, opcaoId, request);

            return NoContent();
        }

        [HttpDelete("{opcaoId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Remover([FromServices] IRemoverOpcaoUseCase useCase, [FromRoute] int produtoId, [FromRoute] int opcaoId)
        {
            await useCase.Execute(produtoId, opcaoId);

            return NoContent();
        }

        [HttpPatch("{opcaoId:int}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AlterarStatus([FromServices] IAlterarStatusOpcaoUseCase useCase, [FromRoute] int produtoId, [FromRoute] int opcaoId, [FromBody] RequestAlterarStatus request)
        {
            await useCase.Execute(produtoId, opcaoId, request);

            return NoContent();
        }
    }
}

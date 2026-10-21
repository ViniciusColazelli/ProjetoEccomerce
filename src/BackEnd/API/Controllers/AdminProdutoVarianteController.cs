using Application.Request;
using Application.Response;
using Application.UseCases.ProdutoVariante.AlterarStatus;
using Application.UseCases.ProdutoVariante.Gerar;
using Domain.Security.Perfis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/admin/produto/{produtoId:int}/variante")]
    [ApiController]
    [Authorize(Roles = Perfis.Admin)]
    public class AdminProdutoVarianteController : ControllerBase
    {
        [HttpPost("gerar")]
        [ProducesResponseType(typeof(ResponseVariantesGeradas), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Gerar([FromServices] IGerarVariantesUseCase useCase, [FromRoute] int produtoId)
        {
            var result = await useCase.Execute(produtoId);

            return Ok(result);
        }

        [HttpPatch("{varianteId:int}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AlterarStatus([FromServices] IAlterarStatusVarianteUseCase useCase, [FromRoute] int produtoId, [FromRoute] int varianteId, [FromBody] RequestAlterarStatus request)
        {
            await useCase.Execute(produtoId, varianteId, request);

            return NoContent();
        }
    }
}

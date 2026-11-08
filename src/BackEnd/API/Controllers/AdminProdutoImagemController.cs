using Application.Request;
using Application.Response;
using Application.UseCases.ProdutoImagem.Enviar;
using Application.UseCases.ProdutoImagem.Ordenar;
using Application.UseCases.ProdutoImagem.Remover;
using Domain.Security.Perfis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/admin/produto/{produtoId:int}/imagem")]
    [ApiController]
    [Authorize(Roles = Perfis.Admin)]
    [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status403Forbidden)]
    public class AdminProdutoImagemController : ControllerBase
    {
        // foto (3 MB) + miniatura (1 MB) + o resto do multipart; acima disso o Kestrel recusa com 413
        private const long TAMANHO_MAXIMO_REQUISICAO = 5 * 1024 * 1024;

        // multipart com os campos "imagem" (versão grande) e "miniatura", já reduzidas pelo navegador
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(TAMANHO_MAXIMO_REQUISICAO)]
        [RequestFormLimits(MultipartBodyLengthLimit = TAMANHO_MAXIMO_REQUISICAO)]
        [ProducesResponseType(typeof(ResponseProdutoImagem), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Enviar([FromServices] IEnviarImagemUseCase useCase, [FromRoute] int produtoId, IFormFile? imagem, IFormFile? miniatura)
        {
            await using var conteudoImagem = imagem?.OpenReadStream();
            await using var conteudoMiniatura = miniatura?.OpenReadStream();

            var request = new RequestEnviarImagem
            {
                Imagem = conteudoImagem is null ? null : new ArquivoEnviado { Conteudo = conteudoImagem, Tamanho = imagem!.Length },
                Miniatura = conteudoMiniatura is null ? null : new ArquivoEnviado { Conteudo = conteudoMiniatura, Tamanho = miniatura!.Length }
            };

            var result = await useCase.Execute(produtoId, request);

            return Created(string.Empty, result);
        }

        [HttpDelete("{imagemId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Remover([FromServices] IRemoverImagemUseCase useCase, [FromRoute] int produtoId, [FromRoute] int imagemId)
        {
            await useCase.Execute(produtoId, imagemId);

            return NoContent();
        }

        // Body: { ids: [...] } com todas as fotos na ordem nova; devolve a lista já ordenada
        [HttpPut("ordem")]
        [ProducesResponseType(typeof(IList<ResponseProdutoImagem>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErro), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Ordenar([FromServices] IOrdenarImagensUseCase useCase, [FromRoute] int produtoId, [FromBody] RequestOrdenarImagens request)
        {
            var result = await useCase.Execute(produtoId, request);

            return Ok(result);
        }
    }
}

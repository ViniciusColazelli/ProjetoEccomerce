using Application.Response;
using Domain.Entities;
using Domain.Services.Armazenamento;

namespace Application.Services.Mapeamento
{
    // O banco guarda o caminho; quem monta o endereço é o armazenamento (disco local ou Supabase)
    public static class MapearImagem
    {
        public static ResponseProdutoImagem ParaResponse(ProdutoImagens imagem, IArmazenamentoImagens armazenamento) => new()
        {
            Id = imagem.Id,
            Url = armazenamento.UrlPublica(imagem.Caminho),
            UrlMiniatura = armazenamento.UrlPublica(imagem.CaminhoMiniatura),
            Ordem = imagem.Ordem
        };

        // Galeria na ordem de exibição (a primeira é a capa)
        public static IList<ResponseProdutoImagem> ParaLista(IEnumerable<ProdutoImagens> imagens, IArmazenamentoImagens armazenamento) =>
            imagens
                .OrderBy(imagem => imagem.Ordem)
                .ThenBy(imagem => imagem.Id)
                .Select(imagem => ParaResponse(imagem, armazenamento))
                .ToList();

        // Miniatura da capa para cards e listas; null quando o produto não tem foto (o front mostra o placeholder)
        public static string? CapaMiniatura(IEnumerable<ProdutoImagens> imagens, IArmazenamentoImagens armazenamento)
        {
            var capa = imagens.OrderBy(imagem => imagem.Ordem).ThenBy(imagem => imagem.Id).FirstOrDefault();
            return capa is null ? null : armazenamento.UrlPublica(capa.CaminhoMiniatura);
        }
    }
}

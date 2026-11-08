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
    }
}

using Application.Request;
using Domain.Entities;
using Domain.Enums;
using Domain.Filtros;

namespace Application.Services.Mapeamento
{
    public static class MapearRequest
    {
        public static Clientes RequestParaEntidade(RequestRegistrarCliente request)
        {
            return new Clientes
            {
                Nome = request.Nome,
                Email = request.Email
            };
        }

        public static FiltroListagemProdutos RequestParaFiltro(RequestListarProdutos request)
        {
            return new FiltroListagemProdutos
            {
                Nome = request.Nome,
                OrdenarPor = string.Equals(request.OrdenarPor?.Trim(), "preco", StringComparison.OrdinalIgnoreCase)
                    ? OrdenacaoProduto.Preco
                    : OrdenacaoProduto.Nome,
                Decrescente = string.Equals(request.Direcao?.Trim(), "desc", StringComparison.OrdinalIgnoreCase),
                SomenteDisponiveis = request.SomenteDisponiveis,
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina
            };
        }
    }
}

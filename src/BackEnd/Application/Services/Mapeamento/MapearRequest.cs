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
                Email = request.Email.Trim().ToLowerInvariant()
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

        public static FiltroListagemProdutos RequestParaFiltroAdmin(RequestListarProdutosAdmin request)
        {
            var filtro = RequestParaFiltro(request);

            filtro.Ativo = request.Ativo;

            return filtro;
        }

        public static FiltroListagemEstoque RequestParaFiltro(RequestListarEstoque request)
        {
            return new FiltroListagemEstoque
            {
                ProdutoId = request.ProdutoId,
                SomenteZerados = request.SomenteZerados,
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina
            };
        }

        public static FiltroListagemMovimentacoes RequestParaFiltro(RequestListarMovimentacoes request)
        {
            return new FiltroListagemMovimentacoes
            {
                VarianteId = request.VarianteId,
                ProdutoId = request.ProdutoId,
                PedidoId = request.PedidoId,
                Tipo = string.IsNullOrWhiteSpace(request.Tipo)
                    ? null
                    : Enum.Parse<TipoMovimentacao>(request.Tipo.Trim(), ignoreCase: true),
                De = request.De,
                Ate = request.Ate,
                Pagina = request.Pagina,
                TamanhoPagina = request.TamanhoPagina
            };
        }

        public static Produtos RequestParaEntidade(RequestRegistrarProduto request)
        {
            return new Produtos
            {
                Nome = request.Nome,
                Descricao = request.Descricao,
                PrecoBase = request.PrecoBase,
                Ativo = true
            };
        }

        public static ProdutoOpcoes RequestParaEntidade(RequestRegistrarOpcao request, int produtoId)
        {
            return new ProdutoOpcoes
            {
                ProdutoId = produtoId,
                Tipo = Enum.Parse<TipoOpcao>(request.Tipo, ignoreCase: true),
                Valor = request.Valor,
                PrecoAdicional = request.PrecoAdicional,
                Ativo = true
            };
        }
    }
}

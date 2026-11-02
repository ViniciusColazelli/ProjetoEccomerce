using Application.Services.MovimentarEstoque;
using Application.UseCases.EstoqueVariante.Ajustar;
using Application.UseCases.EstoqueVariante.Entrada;
using Application.UseCases.EstoqueVariante.Historico;
using Application.UseCases.EstoqueVariante.Listar;
using Application.UseCases.EstoqueVariante.Obter;
using Application.UseCases.EstoqueVariante.Saida;
using Application.UseCases.Login;
using Application.UseCases.Produto.AlterarStatus;
using Application.UseCases.Produto.Listar;
using Application.UseCases.Produto.ListarAdmin;
using Application.UseCases.Produto.Obter;
using Application.UseCases.Produto.ObterAdmin;
using Application.UseCases.Produto.Registrar;
using Application.UseCases.Produto.Update;
using Application.UseCases.ProdutoOpcao.AlterarStatus;
using Application.UseCases.ProdutoOpcao.Registrar;
using Application.UseCases.ProdutoOpcao.Remover;
using Application.UseCases.ProdutoOpcao.Update;
using Application.UseCases.ProdutoVariante.AlterarStatus;
using Application.UseCases.ProdutoVariante.Gerar;
using Application.UseCases.Profile;
using Application.UseCases.Registrar;
using Application.UseCases.TrocarSenha;
using Application.UseCases.Update;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class InjecaoDeDependenciaExtension
    {
        public static void AddApplication(this IServiceCollection services)
        {
            AddUseCases(services);
            AddServices(services);
        }

        private static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IMovimentarEstoque, MovimentarEstoque>();
        }

        private static void AddUseCases(this IServiceCollection services)
        {
            services.AddScoped<IRegistrarClienteUseCase, ResgitrarClienteUseCase>();
            services.AddScoped<ILoginClienteUseCase, LoginClienteUseCase>();
            services.AddScoped<IUpdateClienteUseCase, UpdateClienteUseCase>();
            services.AddScoped<ITrocarSenhaUseCase, TrocarSenhaUseCase>();
            services.AddScoped<IGetClienteProfileUseCase, GetClienteProfileUseCase>();
            services.AddScoped<IListarProdutosUseCase, ListarProdutosUseCase>();
            services.AddScoped<IObterProdutoUseCase, ObterProdutoUseCase>();
            services.AddScoped<IListarProdutosAdminUseCase, ListarProdutosAdminUseCase>();
            services.AddScoped<IObterProdutoAdminUseCase, ObterProdutoAdminUseCase>();
            services.AddScoped<IRegistrarProdutoUseCase, RegistrarProdutoUseCase>();
            services.AddScoped<IUpdateProdutoUseCase, UpdateProdutoUseCase>();
            services.AddScoped<IAlterarStatusProdutoUseCase, AlterarStatusProdutoUseCase>();
            services.AddScoped<IRegistrarOpcaoUseCase, RegistrarOpcaoUseCase>();
            services.AddScoped<IUpdateOpcaoUseCase, UpdateOpcaoUseCase>();
            services.AddScoped<IRemoverOpcaoUseCase, RemoverOpcaoUseCase>();
            services.AddScoped<IAlterarStatusOpcaoUseCase, AlterarStatusOpcaoUseCase>();
            services.AddScoped<IGerarVariantesUseCase, GerarVariantesUseCase>();
            services.AddScoped<IAlterarStatusVarianteUseCase, AlterarStatusVarianteUseCase>();
            services.AddScoped<IListarEstoqueUseCase, ListarEstoqueUseCase>();
            services.AddScoped<IObterEstoqueUseCase, ObterEstoqueUseCase>();
            services.AddScoped<IEntradaEstoqueUseCase, EntradaEstoqueUseCase>();
            services.AddScoped<ISaidaEstoqueUseCase, SaidaEstoqueUseCase>();
            services.AddScoped<IAjustarEstoqueUseCase, AjustarEstoqueUseCase>();
            services.AddScoped<IListarMovimentacoesUseCase, ListarMovimentacoesUseCase>();

        }
    }
}

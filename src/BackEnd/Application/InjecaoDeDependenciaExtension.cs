using Application.UseCases.Login;
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
        }

        private static void AddUseCases(this IServiceCollection services)
        {
            services.AddScoped<IRegistrarClienteUseCase, ResgitrarClienteUseCase>();
            services.AddScoped<ILoginClienteUseCase, LoginClienteUseCase>();
            services.AddScoped<IUpdateClienteUseCase, UpdateClienteUseCase>();
            services.AddScoped<ITrocarSenhaUseCase, TrocarSenhaUseCase>();
            services.AddScoped<IGetClienteProfileUseCase, GetClienteProfileUseCase>();

        }
    }
}

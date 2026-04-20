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

        }
    }
}

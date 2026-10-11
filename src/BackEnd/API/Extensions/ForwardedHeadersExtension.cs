using Microsoft.AspNetCore.HttpOverrides;

namespace API.Extensions
{
    public static class ForwardedHeadersExtension
    {
        public static void AddForwardedHeadersConfigured(this IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }
    }
}
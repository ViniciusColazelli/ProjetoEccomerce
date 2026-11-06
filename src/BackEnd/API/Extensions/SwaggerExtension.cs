using Microsoft.OpenApi;

namespace API.Extensions
{
    public static class SwaggerExtension
    {
        private const string ESQUEMA_JWT = "Bearer";

        public static void AddSwaggerComJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition(ESQUEMA_JWT, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Informe apenas o token, sem o prefixo 'Bearer '."
                });

                // Microsoft.OpenApi 2: a referência ao esquema é criada a partir do documento que está sendo gerado
                options.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(ESQUEMA_JWT, documento)] = []
                });
            });
        }
    }
}

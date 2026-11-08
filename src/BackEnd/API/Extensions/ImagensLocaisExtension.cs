using Infrastructure.Services.Armazenamento;
using Microsoft.Extensions.FileProviders;

namespace API.Extensions
{
    public static class ImagensLocaisExtension
    {
        // Com o provedor Local, a API serve as fotos da pasta em /imagens (com o Supabase, o navegador busca
        // direto do CDN dele e nada é servido aqui). Fica antes do rate limit: uma página de produtos
        // carrega várias fotos de uma vez.
        public static void UseImagensLocais(this WebApplication app)
        {
            var config = app.Services.GetRequiredService<ArmazenamentoConfig>();
            if (config.UsaSupabase)
                return;

            // no Render o disco é apagado a cada deploy: lá o provedor precisa ser o Supabase
            if (app.Environment.IsDevelopment() == false && app.Environment.IsEnvironment("Testing") == false)
                app.Logger.LogWarning("Storage:Provedor=Local fora de Development: as fotos somem no próximo deploy. Configure o Supabase Storage.");

            var pasta = config.PastaLocalAbsoluta(app.Environment.ContentRootPath);
            Directory.CreateDirectory(pasta);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(pasta),
                RequestPath = ArmazenamentoConfig.RotaImagensLocais
            });
        }
    }
}

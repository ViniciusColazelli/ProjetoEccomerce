using API.Converters;
using API.Extensions;
using API.Filtros;
using Application;
using Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseEnvironmentPort(); // implementação na extensão HostingExtension.cs

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new StringConverter());

    // Enums trafegam pelo nome ("Tamanho", "Entrada") e não pelo número
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerComJwt();

builder.Services.AddMvc(opcao => opcao.Filters.Add(typeof(ExceptionFiltro)));

builder.Services.AddHttpContextAccessor();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRateLimitConfigurado();
builder.Services.AddForwardedHeadersConfigured();
builder.Services.AddCorsConfigured(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// fotos dos produtos quando o Storage é o disco local (implementação em ImagensLocaisExtension.cs)
app.UseImagensLocais();

app.UseCors(CorsExtension.PoliticaFrontEnd);

// Em Development o front chama a API pelo proxy do Next, que manda X-Forwarded-Proto: http:
// redirecionar mandaria o navegador direto para a API, sem o cookie de sessão
if (app.Environment.IsDevelopment() == false)
    app.UseHttpsRedirection();

app.UseAuthentication();

// Nos testes de integração todas as requisições vêm do mesmo IP e estourariam o limite
if (app.Environment.IsEnvironment("Testing") == false)
    app.UseRateLimiter();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("ok")).DisableRateLimiting();

app.MapControllers();

app.Services.MigrateDatabase();

app.Run();

public partial class Program { }

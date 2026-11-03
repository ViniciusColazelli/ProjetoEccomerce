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

app.UseCors(CorsExtension.PoliticaFrontEnd);

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

using Microsoft.AspNetCore.HttpOverrides;
using Pos.Api.Extensions;
using Pos.Application;
using Pos.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("POS API"));
}
else
{
    app.UseHsts();
}

app.UseResponseCompression();
app.UseCors(ServiceCollectionExtensions.CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

await app.InitializeDatabaseAsync();
await app.RunAsync();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;

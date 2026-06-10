using Aura.Api.Endpoints;
using Aura.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuraInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapGet("/", () => "Hello World!");

app.MapAuthEndpoints(app.Environment);

app.Run();

namespace Aura.Api
{
    /// <summary>
    /// Marker type for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
    /// Lives in the Api assembly so the factory can locate the entry point.
    /// </summary>
    public sealed class ApiMarker;
}

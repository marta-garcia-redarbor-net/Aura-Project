using Aura.UI.Components;
using Aura.UI.Services;

namespace Aura.UI;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ForwardedAccessTokenHandler>();

        // DEV-ONLY: auto-acquire a mock JWT so the UI can call protected API endpoints
        // without a real browser token. Remove when real auth (e.g. MSAL) is wired up.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddTransient<DevAccessTokenHandler>();
        }

        var httpClientBuilder = builder.Services
            .AddHttpClient<IDashboardApiClient, DashboardApiClient>((serviceProvider, client) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["AuraApi:BaseUrl"] ?? "http://localhost:5180";

                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<ForwardedAccessTokenHandler>();

        var graphHttpClientBuilder = builder.Services
            .AddHttpClient<IGraphConnectorApiClient, GraphConnectorApiClient>((serviceProvider, client) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["AuraApi:BaseUrl"] ?? "http://localhost:5180";

                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<ForwardedAccessTokenHandler>();

        var systemStatusHttpClientBuilder = builder.Services
            .AddHttpClient<ISystemStatusApiClient, SystemStatusApiClient>((serviceProvider, client) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["AuraApi:BaseUrl"] ?? "http://localhost:5180";

                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<ForwardedAccessTokenHandler>();

        var moduleProgressHttpClientBuilder = builder.Services
            .AddHttpClient<IModuleProgressApiClient, ModuleProgressApiClient>((serviceProvider, client) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var baseUrl = configuration["AuraApi:BaseUrl"] ?? "http://localhost:5180";

                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddHttpMessageHandler<ForwardedAccessTokenHandler>();

        if (builder.Environment.IsDevelopment())
        {
            httpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            graphHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            systemStatusHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            moduleProgressHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
        }

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}

/// <summary>
/// Marker type for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class UiMarker;

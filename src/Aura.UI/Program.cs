using Aura.UI.Components;
using Aura.UI.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

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

        var useEntraId = builder.Configuration.GetValue<bool>("UseEntraId");

        // Always register CascadingAuthenticationState so App.razor's wrapper works in all modes.
        // The backing AuthenticationStateProvider varies by mode:
        // - UseEntraId=true: OIDC-backed provider from AddMicrosoftIdentityWebApp
        // - UseEntraId=false (dev): cookie-based provider (anonymous by default)
        builder.Services.AddCascadingAuthenticationState();

        if (useEntraId)
        {
            // OIDC pipeline: Entra ID interactive browser auth via Microsoft.Identity.Web
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

            builder.Services.AddAuthorization();
        }
        else
        {
            // DEV-ONLY: auto-acquire a mock JWT so the UI can call protected API endpoints
            // without a real browser token. Remove when real auth (e.g. MSAL) is wired up.
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddTransient<DevAccessTokenHandler>();
                builder.Services.AddScoped<ITokenAcquisitionService, DevTokenAcquisitionService>();
            }
            else
            {
                // Register MSAL-based token acquisition for production
                builder.Services.AddScoped<ITokenAcquisitionService, MsalTokenAcquisitionService>();
                
                // Register MSAL PublicClientApplication for interactive browser auth
                builder.Services.AddSingleton<IPublicClientApplication>(provider =>
                {
                    var configuration = provider.GetRequiredService<IConfiguration>();
                    var clientId = configuration["AzureAd:ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
                    var tenantId = configuration["AzureAd:TenantId"] ?? throw new InvalidOperationException("AzureAd:TenantId not configured");
                    
                    return PublicClientApplicationBuilder
                        .Create(clientId)
                        .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                        .WithRedirectUri("http://localhost:5000/authentication/login-callback")
                        .Build();
                });
            }
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

        var dashboardPreviewHttpClientBuilder = builder.Services
            .AddHttpClient<IDashboardPreviewApiClient, DashboardPreviewApiClient>((serviceProvider, client) =>
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
            dashboardPreviewHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
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

        if (useEntraId)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}

/// <summary>
/// Marker type for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class UiMarker;

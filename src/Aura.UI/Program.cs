using System.Security.Claims;
using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Domain.Calendar;
using Aura.UI.Components;
using Aura.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Identity.Client;

namespace Aura.UI;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<ForwardedAccessTokenHandler>();

        var useEntraId = builder.Configuration.GetValue<bool>("UseEntraId");

        // Always register CascadingAuthenticationState so App.razor's wrapper works in all modes.
        // The backing AuthenticationStateProvider varies by mode:
        // - UseEntraId=true: OIDC-backed provider from AddMicrosoftIdentityWebApp
        // - UseEntraId=false (dev): cookie-based provider (anonymous by default)
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IAuthPopupService, AuthPopupService>();

        if (useEntraId)
        {
            // OIDC pipeline: Authorization Code flow via challenge endpoint.
            // The middleware owns state/nonce/correlation — no manual URL construction.
            var azureAd = builder.Configuration.GetSection("AzureAd");
            var clientId = azureAd["ClientId"] ?? throw new InvalidOperationException("AzureAd:ClientId not configured");
            var tenantId = azureAd["TenantId"] ?? throw new InvalidOperationException("AzureAd:TenantId not configured");
            var clientSecret = azureAd["ClientSecret"] ?? throw new InvalidOperationException("AzureAd:ClientSecret not configured");

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Events.OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    };
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                    // Force v2.0 metadata discovery to avoid v1.0 fallback
                    options.MetadataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.ResponseType = "code";
                    options.CallbackPath = "/signin-oidc";
                    options.SaveTokens = true;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    // Request the API scope so the access_token saved by SaveTokens
                    // has audience api://{clientId} — required by the API JWT Bearer validator.
                    options.Scope.Add($"api://{clientId}/MeetingAlerts");
                    // Graph delegated scopes — consented during OIDC login.
                    // The access token for Graph is acquired separately via refresh token
                    // in OnTokenValidated and cached in the MSAL SQLite user token cache.
                    //options.Scope.Add("Mail.Read");
                    //options.Scope.Add("Chat.Read");
                    //options.Scope.Add("Calendars.Read");
                    options.Scope.Add("User.Read");

                    // Add resource parameter so Entra ID returns an access_token
                    // for our API instead of the default Microsoft Graph token.
                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.SetParameter("resource", $"api://{clientId}");
                        return Task.CompletedTask;
                    };

                    // After OIDC login, acquire Graph tokens using the refresh token
                    // and cache them in the MSAL SQLite user token cache.
                    // This allows GraphClientFactory.AcquireTokenSilent to find cached
                    // tokens for delegated Graph operations (sync, workers, etc.).
                    options.Events.OnTokenValidated = async context =>
                    {
                        var refreshToken = context.TokenEndpointResponse?.RefreshToken;
                        if (string.IsNullOrEmpty(refreshToken))
                            return;

                        try
                        {
                            var msalApp = context.HttpContext.RequestServices
                                .GetRequiredService<IPublicClientApplication>();

                            var graphScopes = new[]
                            {
                                "Mail.Read", "Chat.Read", "Calendars.Read", "User.Read"
                            };

                            // AcquireTokenByRefreshToken is an explicit interface
                            // implementation on IByRefreshToken. The refresh token is
                            // a direct parameter, not chained via WithRefreshToken.
                            var byRefreshToken = (IByRefreshToken)msalApp;
                            var result = await byRefreshToken
                                .AcquireTokenByRefreshToken(graphScopes, refreshToken)
                                .ExecuteAsync();

                            var loggerFactory = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>();
                            var logger = loggerFactory.CreateLogger("Aura.UI.OidcGraph");
                            logger.LogInformation(
                                "Graph tokens cached in MSAL user token cache " +
                                "(account={AccountId})",
                                result.Account?.HomeAccountId.ObjectId);
                        }
                        catch (Exception ex)
                        {
                            var loggerFactory = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>();
                            var logger = loggerFactory.CreateLogger("Aura.UI.OidcGraph");
                            logger.LogWarning(ex,
                                "Failed to acquire Graph tokens via refresh token. " +
                                "Sync will fail until Graph tokens are cached.");
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // Register IConfidentialClientApplication for client-credentials fallback.
            // When SaveTokens=true doesn't produce an access_token (e.g. MeetingAlerts scope
            // not yet registered in Entra ID), ForwardedAccessTokenHandler falls back to
            // client credentials to obtain a valid bearer token for the API.
            builder.Services.AddSingleton<IConfidentialClientApplication>(
                ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .Build());

            // Register ITokenAcquisitionService for components that need it (e.g. MeetingAlertToast)
            builder.Services.AddScoped<ITokenAcquisitionService, MsalTokenAcquisitionService>();
        }
        else
        {
            // CRITICAL-01: Register cookie authentication so HttpContext.SignInAsync("Cookies", ...)
            // works in RestrictedAccessView.HandleDevLogin(). Without this, the sign-in call silently
            // fails because no "Cookies" authentication scheme is registered.
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie();

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
            }
        }

        var apiBaseUrl = builder.Configuration["AuraApi:BaseUrl"] ?? "http://localhost:5180";

        static IHttpClientBuilder AddApiHttpClient<TClient, TInterface>(
            IServiceCollection services, string baseUrl)
            where TClient : class, TInterface
            where TInterface : class
            => services.AddHttpClient<TInterface, TClient>((_, client) =>
            {
                client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(10);
            }).AddHttpMessageHandler<ForwardedAccessTokenHandler>();

        var httpClientBuilder = AddApiHttpClient<DashboardApiClient, IDashboardApiClient>(builder.Services, apiBaseUrl);
        var graphHttpClientBuilder = AddApiHttpClient<GraphConnectorApiClient, IGraphConnectorApiClient>(builder.Services, apiBaseUrl);
        var dashboardPreviewHttpClientBuilder = AddApiHttpClient<DashboardPreviewApiClient, IDashboardPreviewApiClient>(builder.Services, apiBaseUrl);
        var systemStatusHttpClientBuilder = AddApiHttpClient<SystemStatusApiClient, ISystemStatusApiClient>(builder.Services, apiBaseUrl);
        var moduleProgressHttpClientBuilder = AddApiHttpClient<ModuleProgressApiClient, IModuleProgressApiClient>(builder.Services, apiBaseUrl);
        var syncHttpClientBuilder = AddApiHttpClient<SyncApiClient, ISyncApiClient>(builder.Services, apiBaseUrl);
        var calendarHttpClientBuilder = AddApiHttpClient<CalendarApiClient, ICalendarApiClient>(builder.Services, apiBaseUrl);
        calendarHttpClientBuilder.AddStandardResilienceHandler();
        var workItemsHttpClientBuilder = AddApiHttpClient<WorkItemsApiClient, IWorkItemsApiClient>(builder.Services, apiBaseUrl);

        // Calendar use case — dashboard display only
        builder.Services.AddSingleton<ICalendarEventStore, InMemoryCalendarEventStore>();
        builder.Services.AddScoped<GetUpcomingMeetingsUseCase>();

        // Priority Summary — composes preview + calendar into source-based cards
        builder.Services.AddScoped<IPrioritySummaryService, PrioritySummaryService>();

        if (!useEntraId && builder.Environment.IsDevelopment())
        {
            httpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            graphHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            dashboardPreviewHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            systemStatusHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            moduleProgressHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            syncHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            calendarHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            workItemsHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
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

        // CRITICAL-03: Authentication/Authorization middleware must always be registered,
        // regardless of UseEntraId. When UseEntraId=false, cookie authentication is registered
        // in the else block above, and the middleware is required for cookie sign-in/claims
        // population to work. Without it, [Authorize] attributes fail even after successful sign-in.
        app.UseAuthentication();
        app.UseAuthorization();

        // OIDC challenge endpoint: opens the popup flow by triggering an OIDC challenge.
        // The OIDC middleware owns state/nonce/correlation — no manual URL construction needed.
        app.MapGet("/login/challenge", async (HttpContext ctx) =>
            await ctx.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = "/authentication/callback" }))
            .AllowAnonymous();

        // Sign-out endpoint: performs server-side sign-out then redirects home.
        // Blazor Server components cannot call SignOutAsync directly because the HTTP
        // response has already started (SignalR circuit). This minimal endpoint runs
        // in a fresh HTTP request where redirect is valid.
        app.MapGet("/logout", async (HttpContext ctx, bool? useEntraId) =>
        {
            if (useEntraId == true)
            {
                // OIDC scheme triggers Entra ID end-session redirect.
                // Wrap in try-catch: if the OIDC config is incomplete or
                // the end-session endpoint is unreachable, fall through
                // to cookie-only sign-out so the user is still logged out locally.
                try
                {
                    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                }
                catch (InvalidOperationException)
                {
                    // OIDC end-session redirect failed — proceed with cookie clear.
                }
            }

            // Cookie scheme clears the local session cookie.
            // Always sign out from cookie regardless of mode.
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ctx.Response.Redirect("/");
        }).AllowAnonymous();

        // Dev login endpoint: performs the sign-in in a fresh HTTP request so cookie
        // auth works. Blazor Server interactive mode cannot call SignInAsync because
        // the HTTP response has already started (SignalR circuit).
        if (!useEntraId)
        {
            app.MapGet("/login/dev", async (HttpContext ctx, IConfiguration config) =>
            {
                var apiBaseUrl = config["AuraApi:BaseUrl"] ?? "http://localhost:5180";

                using var httpClient = new HttpClient();
                var response = await httpClient.PostAsync($"{apiBaseUrl}/api/auth/mock-login", null);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var json = System.Text.Json.JsonDocument.Parse(content);
                var token = json.RootElement.GetProperty("token").GetString()
                    ?? throw new InvalidOperationException("Token not found in response");

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, "dev-user"),
                    new Claim(ClaimTypes.NameIdentifier, "mock-user-001"),
                    new Claim("oid", "mock-user-001"),
                    new Claim("token", token)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                ctx.Response.Redirect("/test-dashboard");
            }).AllowAnonymous();
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

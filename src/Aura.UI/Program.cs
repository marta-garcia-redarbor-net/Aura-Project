using System.Security.Claims;
using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Domain.Calendar;
using Aura.UI.Components;
using Aura.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
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

        builder.Services.AddSingleton<AppVersionService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<ForwardedAccessTokenHandler>();
        builder.Services.AddSingleton<IFocusStateRefreshScheduler, TimerFocusStateRefreshScheduler>();

        // Always register CascadingAuthenticationState so App.razor's wrapper works in all modes.
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<IAuthPopupService, AuthPopupService>();

        // ── Cookie authentication — always registered ──
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        // ── OIDC — registered conditionally on AzureAd config presence ──
        var azureAd = builder.Configuration.GetSection("AzureAd");
        var clientId = azureAd["ClientId"];
        var tenantId = azureAd["TenantId"];
        var clientSecret = azureAd["ClientSecret"];
        var hasAzureAdConfig = !string.IsNullOrEmpty(clientId)
                               && !string.IsNullOrEmpty(tenantId)
                               && !string.IsNullOrEmpty(clientSecret);

        if (hasAzureAdConfig)
        {
            // OIDC pipeline: Authorization Code flow via challenge endpoint.
            builder.Services
                .AddAuthentication()
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
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
                    options.Scope.Add($"api://{clientId}/MeetingAlerts");
                    options.Scope.Add("User.Read");

                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.SetParameter("resource", $"api://{clientId}");
                        return Task.CompletedTask;
                    };

                    options.Events.OnTokenValidated = async context =>
                    {
                        // Persist the OIDC access_token as a claim on the cookie identity.
                        // ForwardedAccessTokenHandler step 3 reads this claim, which
                        // works reliably in both server-rendered pages and Blazor circuits.
                        var oidcToken = context.TokenEndpointResponse?.AccessToken;
                        if (!string.IsNullOrEmpty(oidcToken))
                        {
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            identity?.AddClaim(new Claim("token", oidcToken));
                        }

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

            // Register IConfidentialClientApplication for client-credentials fallback.
            builder.Services.AddSingleton<IConfidentialClientApplication>(
                ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                    .Build());
        }

        // ── Token acquisition service ──
        // In dev, use DevTokenAcquisitionService; otherwise MSAL-based.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddTransient<DevAccessTokenHandler>();
            builder.Services.AddScoped<ITokenAcquisitionService, DevTokenAcquisitionService>();
        }
        else
        {
            builder.Services.AddScoped<ITokenAcquisitionService, MsalTokenAcquisitionService>();
        }

        builder.Services.AddAuthorization();

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
        var focusStateHttpClientBuilder = AddApiHttpClient<FocusStateApiClient, IFocusStateApiClient>(builder.Services, apiBaseUrl);
        var syncHttpClientBuilder = AddApiHttpClient<SyncApiClient, ISyncApiClient>(builder.Services, apiBaseUrl);
        var calendarHttpClientBuilder = AddApiHttpClient<CalendarApiClient, ICalendarApiClient>(builder.Services, apiBaseUrl);
        var workItemsHttpClientBuilder = AddApiHttpClient<WorkItemsApiClient, IWorkItemsApiClient>(builder.Services, apiBaseUrl);
        var decisionLogHttpClientBuilder = AddApiHttpClient<DecisionLogApiClient, IDecisionLogApiClient>(builder.Services, apiBaseUrl);
        var pullRequestsHttpClientBuilder = AddApiHttpClient<PullRequestsApiClient, IPullRequestsApiClient>(builder.Services, apiBaseUrl);
        builder.Services.AddHttpClient("AuraApi", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<ForwardedAccessTokenHandler>();

        // Calendar use case — dashboard display only
        builder.Services.AddSingleton<ICalendarEventStore, InMemoryCalendarEventStore>();
        builder.Services.AddScoped<GetUpcomingMeetingsUseCase>();

        // Priority Summary — composes preview + calendar + PRs into source-based cards
        builder.Services.AddScoped<IPrioritySummaryService, PrioritySummaryService>();

        // Dashboard event bus — singleton to share events and item tracking across all components
        builder.Services.AddSingleton<Aura.UI.Services.IDashboardEventBus, Aura.UI.Services.DashboardEventBus>();
        builder.Services.AddSingleton<Aura.UI.Services.IDashboardRealtimeStatus, Aura.UI.Services.DashboardRealtimeStatus>();

        // PR Review connector — v1 mock client for Azure DevOps PRs
        builder.Services.AddScoped<IAzureDevOpsPrClient, AzureDevOpsPrClient>();
        builder.Services.AddScoped<DemoUiState>();
        builder.Services.AddScoped<SessionExpiredService>();
        builder.Services.AddScoped<TelemetryClient>();

        // Dev access token handler — only in development
        if (builder.Environment.IsDevelopment())
        {
            httpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            graphHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            dashboardPreviewHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            systemStatusHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            moduleProgressHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            focusStateHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            syncHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            calendarHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            workItemsHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            decisionLogHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
            pullRequestsHttpClientBuilder.AddHttpMessageHandler<DevAccessTokenHandler>();
        }

        // ACA/Load Balancer proxy: trust X-Forwarded-Proto and X-Forwarded-Host
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        app.UseForwardedHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseAuthentication();
        app.UseAuthorization();

        // OIDC challenge endpoint: opens the popup flow by triggering an OIDC challenge.
        app.MapGet("/login/challenge", async (HttpContext ctx) =>
        {
            var redirect = ctx.Request.Query.ContainsKey("popup")
                ? "/authentication/callback?popup=true"
                : "/authentication/callback";

            await ctx.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = redirect });
        }).AllowAnonymous();

        // ── Logout — signs out from both OIDC and Cookie unconditionally ──
        app.MapGet("/logout", async (HttpContext ctx) =>
        {
            // OIDC scheme triggers Entra ID end-session redirect.
            // Wrap in try-catch: if OIDC is not configured or the end-session
            // endpoint is unreachable, fall through to cookie-only sign-out.
            try
            {
                await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }
            catch (InvalidOperationException)
            {
                // OIDC end-session redirect failed — proceed with cookie clear.
            }

            // Cookie scheme clears the local session cookie. Always executed.
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ctx.Response.Redirect("/");
        }).AllowAnonymous();

        // ── Dev login endpoint — always available ──
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
            ctx.Response.Redirect("/dashboard");
        }).AllowAnonymous();

        // ── Demo login endpoint — always available ──
        app.MapGet("/login/demo", async (HttpContext ctx, IConfiguration config) =>
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "Demo User"),
                new(ClaimTypes.Email, "demo@aura.local"),
                new(ClaimTypes.Role, "Demo"),
                new("aura_demo_mode", "true"),
                new("oid", "demo-user-001")
            };

            var demoLogger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Aura.UI.DemoLogin");
            var apiBaseUrl = config["AuraApi:BaseUrl"] ?? "http://localhost:5180";
            string? token = null;
            for (var attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    var response = await httpClient.PostAsync($"{apiBaseUrl}/api/auth/mock-login", null);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    using var json = System.Text.Json.JsonDocument.Parse(content);
                    token = json.RootElement.GetProperty("token").GetString();
                    if (token is not null)
                    {
                        demoLogger.LogInformation("Demo login: mock JWT acquired on attempt {Attempt}", attempt);
                        break;
                    }
                }
                catch
                {
                    if (attempt < 10)
                        await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            if (token is null)
            {
                demoLogger.LogWarning("Demo login: failed to acquire mock JWT after 10 attempts");
            }
            else
            {
                claims.Add(new("token", token));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            ctx.Response.Redirect("/dashboard");
        }).AllowAnonymous();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}

/// <summary>
/// Marker type for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class UiMarker;

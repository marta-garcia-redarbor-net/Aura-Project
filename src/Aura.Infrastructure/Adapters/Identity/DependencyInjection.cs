using System.Text;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// DI registration for the identity adapter.
/// Configures dual JWT Bearer authentication (EntraId + MockJwt), mock token generation,
/// and the <see cref="ICurrentUserService"/> port implementation.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers identity-related services: dual JWT Bearer schemes, mock JWT generator
    /// (all environments), and the <see cref="ICurrentUserService"/> adapter backed by
    /// <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/>.
    /// </summary>
    internal static IServiceCollection AddIdentityAdapter(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // Bind options
        services.Configure<MockJwtOptions>(
            configuration.GetSection(MockJwtOptions.SectionName));

        services.Configure<EntraIdOptions>(
            configuration.GetSection(EntraIdOptions.SectionName));

        // ── Dual JWT Bearer scheme registration ──
        // Both schemes are registered simultaneously. The default policy accepts either.
        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = "EntraId";
            options.DefaultAuthenticateScheme = "EntraId";
        });

        // ── EntraId scheme — registered when AzureAd config is present ──
        var azureAdSection = configuration.GetSection(EntraIdOptions.SectionName);
        var entraIdOptions = azureAdSection.Get<EntraIdOptions>();
        var hasAzureAdConfig = !string.IsNullOrEmpty(entraIdOptions?.ClientId)
                               && !string.IsNullOrEmpty(entraIdOptions?.TenantId);

        if (hasAzureAdConfig)
        {
            var metadataAddress = $"https://login.microsoftonline.com/{entraIdOptions.TenantId}/v2.0/.well-known/openid-configuration";

            var validAudiences = configuration
                .GetSection("EntraId:ValidAudiences")
                .Get<string[]>()
                ?? [$"api://{entraIdOptions.ClientId}", entraIdOptions.ClientId];

            authBuilder.AddJwtBearer("EntraId", options =>
            {
                options.MetadataAddress = metadataAddress;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuers =
                    [
                        $"https://login.microsoftonline.com/{entraIdOptions.TenantId}/v2.0",
                        $"https://sts.windows.net/{entraIdOptions.TenantId}/"
                    ],
                    ValidAudiences = validAudiences
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(context.Exception,
                            "JWT VALIDATION FAILED: {ExceptionMessage}. " +
                            "Token issuer: {Issuer}, Token audience: {Audience}. " +
                            "Configured audiences: {ConfiguredAudiences}",
                            context.Exception.Message,
                            context.Principal?.FindFirst("iss")?.Value ?? "N/A",
                            context.Principal?.FindFirst("aud")?.Value ?? "N/A",
                            string.Join(", ", validAudiences));
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = _ => Task.CompletedTask,
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogDebug(
                            "JWT CHALLENGE triggered. Error: {Error}, ErrorDescription: {Desc}",
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    }
                };
            });
        }
        else
        {
            // No AzureAd config — register a placeholder EntraId scheme that rejects all tokens.
            // This keeps the scheme name registered so the default policy doesn't crash.
            authBuilder.AddJwtBearer("EntraId", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    // Reject all tokens — no valid signing key configured
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("placeholder-key-not-for-validation-32chars!"))
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = _ => Task.CompletedTask,
                    OnTokenValidated = _ => Task.CompletedTask,
                    OnChallenge = _ => Task.CompletedTask
                };
            });
        }

        // ── MockJwt scheme — always registered ──
        var jwtOptions = configuration
            .GetSection(MockJwtOptions.SectionName)
            .Get<MockJwtOptions>() ?? new MockJwtOptions();

        authBuilder.AddJwtBearer("MockJwt", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Key))
            };
        });

        // ── Authorization policies ──
        services.AddAuthorization(options =>
        {
            // Default policy — accepts either scheme (dashboard preview, public data)
            options.DefaultPolicy = new AuthorizationPolicyBuilder("EntraId", "MockJwt")
                .RequireAuthenticatedUser()
                .Build();

            // RequireEntraId — only real Microsoft auth (production data)
            options.AddPolicy("RequireEntraId", policy =>
            {
                policy.AddAuthenticationSchemes("EntraId");
                policy.RequireAuthenticatedUser();
            });

            // DemoOnly — requires MockJwt scheme with role=Demo
            options.AddPolicy("DemoOnly", policy =>
            {
                policy.AddAuthenticationSchemes("MockJwt");
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Demo");
            });
        });

        services.AddHttpContextAccessor();

        // Mock JWT generator — available in ALL environments (design decision: no env guard)
        services.AddSingleton<MockJwtGenerator>();

        // Port implementation
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        return services;
    }
}

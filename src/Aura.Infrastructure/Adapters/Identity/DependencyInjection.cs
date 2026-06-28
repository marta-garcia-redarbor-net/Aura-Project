using System.Text;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// DI registration for the identity adapter.
/// Configures JWT Bearer authentication, mock token generation, and the
/// <see cref="ICurrentUserService"/> port implementation.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers identity-related services: JWT Bearer validation, mock JWT generator
    /// (development only), and the <see cref="ICurrentUserService"/> adapter backed by
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

        var useEntraId = configuration.GetValue<bool>("UseEntraId");

        // JWT Bearer authentication — feature-flagged dual pipeline
        if (useEntraId)
        {
            // OIDC pipeline: validate against Entra ID metadata endpoint.
            // ValidAudiences accepts both api://{clientId} (custom scope tokens) and
            // {clientId} directly (access tokens issued for the app itself).
            var entraIdOptions = configuration
                .GetSection(EntraIdOptions.SectionName)
                .Get<EntraIdOptions>() ?? new EntraIdOptions();

            var metadataAddress = $"https://login.microsoftonline.com/{entraIdOptions.TenantId}/v2.0/.well-known/openid-configuration";

            var validAudiences = configuration
                .GetSection("EntraId:ValidAudiences")
                .Get<string[]>()
                ?? [$"api://{entraIdOptions.ClientId}", entraIdOptions.ClientId];

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MetadataAddress = metadataAddress;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        // Accept both v1.0 and v2.0 issuers from the same tenant.
                        // The resource parameter in the auth request causes Entra ID
                        // to issue tokens via the v1.0 endpoint (sts.windows.net),
                        // while the v2.0 metadata expects login.microsoftonline.com/v2.0.
                        // Both are from the same tenant and equally secure.
                        ValidIssuers =
                        [
                            $"https://login.microsoftonline.com/{entraIdOptions.TenantId}/v2.0",
                            $"https://sts.windows.net/{entraIdOptions.TenantId}/"
                        ],
                        ValidAudiences = validAudiences
                    };

                    // Diagnostic logging for JWT validation failures
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
            // Mock pipeline: symmetric key validation
            var jwtOptions = configuration
                .GetSection(MockJwtOptions.SectionName)
                .Get<MockJwtOptions>() ?? new MockJwtOptions();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
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
        }

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // Mock JWT generator — development only (design decision: env guard in DI)
        if (environment.IsDevelopment())
        {
            services.AddSingleton<MockJwtGenerator>();
        }

        // Port implementation
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        return services;
    }
}

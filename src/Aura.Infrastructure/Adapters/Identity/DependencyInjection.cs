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

        // ── PolicyScheme for intelligent token routing ──
        // Selects the correct scheme based on token structure:
        // - Tokens with "kid" (Key ID) → EntraId (real Microsoft tokens)
        // - Tokens without "kid" → MockJwt (demo/dev tokens)
        services.AddAuthentication("SmartBearer")
            .AddPolicyScheme("SmartBearer", "Smart Bearer Token Router", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("SmartBearer");
                    var authorization = context.Request.Headers.Authorization.FirstOrDefault();
                    
                    if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        logger?.LogDebug("SmartBearer: No Bearer token found, forwarding to EntraId");
                        return "EntraId";
                    }

                    var token = authorization.Substring("Bearer ".Length).Trim();
                    
                    // Try to decode the JWT header to check for "kid"
                    try
                    {
                        var parts = token.Split('.');
                        if (parts.Length >= 2)
                        {
                            var headerJson = System.Text.Encoding.UTF8.GetString(
                                Convert.FromBase64String(PadBase64(parts[0])));
                            
                            if (headerJson.Contains("\"kid\""))
                            {
                                logger?.LogDebug("SmartBearer: Token has kid, forwarding to EntraId");
                                return "EntraId";
                            }
                            else
                            {
                                logger?.LogDebug("SmartBearer: Token has NO kid, forwarding to MockJwt");
                                return "MockJwt";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogDebug(ex, "SmartBearer: Failed to decode JWT header, forwarding to MockJwt");
                        return "MockJwt";
                    }

                    logger?.LogDebug("SmartBearer: Fallback to EntraId");
                    return "EntraId";
                };
            });

        // ── Dual JWT Bearer scheme registration ──
        // Both schemes are registered simultaneously. The policy scheme routes to the correct one.
        var authBuilder = services.AddAuthentication();

        // Helper function to pad base64 strings
        static string PadBase64(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: return base64 + "==";
                case 3: return base64 + "=";
                default: return base64;
            }
        }

        // ── EntraId scheme — registered when AzureAd config is present ──
        var azureAdSection = configuration.GetSection(EntraIdOptions.SectionName);
        var entraIdOptions = azureAdSection.Get<EntraIdOptions>();
        var useEntraId = configuration.GetValue<bool>("UseEntraId");
        var hasAzureAdConfig = useEntraId && !string.IsNullOrWhiteSpace(entraIdOptions?.TenantId);

        if (hasAzureAdConfig)
        {
            var metadataAddress = $"https://login.microsoftonline.com/{entraIdOptions.TenantId}/v2.0/.well-known/openid-configuration";

            var validAudiences = configuration
                .GetSection("EntraId:ValidAudiences")
                .Get<string[]>()
                ?? (string.IsNullOrWhiteSpace(entraIdOptions?.ClientId)
                    ? []
                    : [$"api://{entraIdOptions.ClientId}", entraIdOptions.ClientId]);

            authBuilder.AddJwtBearer("EntraId", options =>
            {
                options.MetadataAddress = metadataAddress;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = validAudiences.Length > 0,
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

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("MockJwt");
                    logger.LogWarning(context.Exception,
                        "MockJwt AUTHENTICATION FAILED: {ExceptionMessage}",
                        context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("MockJwt");
                    logger.LogDebug("MockJwt token validated successfully");

                    var sid = context.Principal?.FindFirst("sid")?.Value;
                    if (string.IsNullOrWhiteSpace(sid))
                    {
                        // Backward compatibility for legacy test tokens without sid.
                        logger.LogDebug("MockJwt: No sid claim, skipping session check");
                        return Task.CompletedTask;
                    }

                    var userId = context.Principal?.FindFirst(EntraIdClaims.ObjectId)?.Value
                                 ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        logger.LogWarning("MockJwt: Missing sid/user claims");
                        context.Fail("MockJwt missing required sid/user claims.");
                        return Task.CompletedTask;
                    }

                    var sessionStore = context.HttpContext.RequestServices.GetRequiredService<IDemoSessionStore>();
                    var nowUtc = DateTimeOffset.UtcNow;
                    if (!sessionStore.IsActive(sid, userId, nowUtc))
                    {
                        logger.LogWarning("MockJwt: Session not active for sid={SessionId}, userId={UserId}", sid, userId);
                        context.Fail("MockJwt session is not active.");
                    }
                    else
                    {
                        logger.LogDebug("MockJwt: Session active for sid={SessionId}, userId={UserId}", sid, userId);
                    }

                    return Task.CompletedTask;
                }
            };
        });

        // ── Authorization policies ──
        services.AddAuthorization(options =>
        {
            // Default policy — uses SmartBearer to route to the correct scheme
            options.DefaultPolicy = new AuthorizationPolicyBuilder("SmartBearer")
                .RequireAuthenticatedUser()
                .Build();

            // RequireEntraId — only real Microsoft auth (production data)
            // Bypasses SmartBearer, uses EntraId directly
            options.AddPolicy("RequireEntraId", policy =>
            {
                policy.AddAuthenticationSchemes("EntraId");
                policy.RequireAuthenticatedUser();
            });

            // RequireEntraOrDemo — uses SmartBearer to route, then checks claims
            options.AddPolicy("RequireEntraOrDemo", policy =>
            {
                policy.AddAuthenticationSchemes("SmartBearer");
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == EntraIdClaims.TenantId)
                    || context.User.IsInRole("Demo"));
            });

            // DemoOnly — requires MockJwt scheme with role=Demo
            // Bypasses SmartBearer, uses MockJwt directly
            options.AddPolicy("DemoOnly", policy =>
            {
                policy.AddAuthenticationSchemes("MockJwt");
                policy.RequireAuthenticatedUser();
                policy.RequireRole("Demo");
            });
        });

        services.AddHttpContextAccessor();

        // Mock JWT generator — available in ALL environments (design decision: no env guard)
        services.AddSingleton<IDemoSessionStore, InMemoryDemoSessionStore>();
        services.AddSingleton<MockJwtGenerator>();

        // Port implementation
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();

        return services;
    }
}

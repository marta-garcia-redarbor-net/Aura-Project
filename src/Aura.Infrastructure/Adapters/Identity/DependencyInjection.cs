using System.Text;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        var jwtOptions = configuration
            .GetSection(MockJwtOptions.SectionName)
            .Get<MockJwtOptions>() ?? new MockJwtOptions();

        // JWT Bearer authentication
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

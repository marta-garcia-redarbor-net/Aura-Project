using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Aura.Api.Endpoints;

/// <summary>
/// Authentication endpoints. Mock login is registered only in development environments.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication-related endpoints under <c>/api/auth</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints,
        IHostEnvironment environment)
    {
        var group = endpoints.MapGroup("/api/auth")
            .RequireRateLimiting("auth");

        // Mock login — development only
        if (environment.IsDevelopment())
        {
            group.MapPost("/mock-login", (MockJwtGenerator generator) =>
            {
                var token = generator.GenerateToken();
                return Results.Ok(new { token });
            })
            .AllowAnonymous()
            .RequireCors("AllowUiOrigin");
        }

        // Protected endpoint — returns the current authenticated user
        group.MapGet("/me", (ICurrentUserService currentUserService) =>
        {
            var user = currentUserService.GetCurrentUser();
            return user is not null ? Results.Ok(user) : Results.Unauthorized();
        })
        .RequireAuthorization();

        return endpoints;
    }
}

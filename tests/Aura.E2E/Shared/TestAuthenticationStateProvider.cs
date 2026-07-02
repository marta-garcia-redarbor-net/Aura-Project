using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aura.E2E.Shared;

internal sealed class TestAuthenticationStateProvider(string userId = "test-user-001")
    : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Test User")
        ],
        authenticationType: "Test");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}

internal static class AuthenticatedUiTestServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticatedUiTestUser(
        this IServiceCollection services,
        string userId = "test-user-001")
    {
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();
        services.RemoveAll<AuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(_ =>
            new TestAuthenticationStateProvider(userId));

        return services;
    }
}

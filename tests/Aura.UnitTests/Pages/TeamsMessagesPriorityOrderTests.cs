using System.Security.Claims;
using Aura.UI.Models;
using Aura.UI.Pages;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Pages;

public class TeamsMessagesPriorityOrderTests : TestContext
{
    private sealed class AlwaysAuthorizedService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    [Fact]
    public void SortsByPriorityScoreDescending()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();

        var api = Substitute.For<IWorkItemsApiClient>();
        api.GetBySourceAsync("TeamsMessage", "Pending", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WorkItemDetailResponse>>(
            [
                new WorkItemDetailResponse(Guid.NewGuid(), "a", "Low", "messages", "TeamsMessage", "Pending", "Low", "1m", DateTimeOffset.UtcNow.AddMinutes(-1)) { PriorityScore = 10 },
                new WorkItemDetailResponse(Guid.NewGuid(), "b", "High", "messages", "TeamsMessage", "Pending", "High", "2m", DateTimeOffset.UtcNow.AddMinutes(-2)) { PriorityScore = 90 }
            ]));
        Services.AddSingleton(api);

        Task<AuthenticationState> authStateTask = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Test")], "TestAuth"))));

        var cut = RenderComponent<TeamsMessages>(p => p.AddCascadingValue(authStateTask));
        cut.WaitForElement("[data-testid='teams-messages-items']");

        var titles = cut.FindAll("[data-testid='teams-message-title']").Select(x => x.TextContent.Trim()).ToList();
        Assert.Equal(["High", "Low"], titles);
    }
}

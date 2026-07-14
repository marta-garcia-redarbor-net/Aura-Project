using Aura.UI.Components.Layout;
using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class HeaderFocusStateBadgeTests : TestContext
{
    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "false"
            })
            .Build();
    }

    private void SetupCommonServices()
    {
        Services.AddSingleton(CreateConfig());
        Services.AddSingleton(new DemoUiState());
        Services.AddSingleton<IDashboardEventBus>(new DashboardEventBus());

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost:5180/")
        };
        httpClientFactory.CreateClient("AuraApi").Returns(httpClient);
        Services.AddSingleton(httpClientFactory);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    [Fact]
    public void Header_RendersFocusStateBadge_WithCurrentState()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("WindowOfOpportunity", false, "user-123")));

        SetupCommonServices();
        Services.AddSingleton(api);

        var previewApi = Substitute.For<IDashboardPreviewApiClient>();
        previewApi.GetPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardPreviewResponse([], [])
            {
                TotalPendingCount = 4,
                HighPriorityCount = 2,
                TopItems = []
            }));
        Services.AddSingleton(previewApi);

        var cut = RenderComponent<Header>();

        cut.WaitForElement("[data-testid='focus-state-badge']");
        Assert.Contains("Window of Opportunity", cut.Markup);
    }

    [Fact]
    public void Header_FocusStateDropdown_SelectingOverride_CallsApiAndUpdatesBadge()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("Recovery", false, "user-123")));

        SetupCommonServices();
        Services.AddSingleton(api);

        var previewApi = Substitute.For<IDashboardPreviewApiClient>();
        previewApi.GetPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardPreviewResponse([], [])
            {
                TotalPendingCount = 6,
                HighPriorityCount = 3,
                TopItems = []
            }));
        Services.AddSingleton(previewApi);

        var cut = RenderComponent<Header>();

        cut.WaitForElement("[data-testid='focus-state-badge']").Click();
        cut.Find("[data-testid='focus-state-option-DeepWork']").Click();

        api.Received(1).SetOverrideAsync("DeepWork", Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => Assert.Contains("Deep Work", cut.Markup));
    }
}

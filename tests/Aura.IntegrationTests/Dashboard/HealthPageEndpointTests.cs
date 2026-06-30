using System.Net;
using Aura.UI;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Dashboard;

/// <summary>
/// Integration tests for the <c>/health</c> Blazor page route.
/// Verifies the route exists and returns HTTP 200.
/// Full component content validation is done via bUnit tests and E2E browser tests.
/// </summary>
public class HealthPageEndpointTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public HealthPageEndpointTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthPage_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        // Blazor Server always returns 200 for known routes;
        // auth is enforced inside the component via AuthorizeView, not at HTTP level.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthPage_ReturnsHtmlContent()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Verify we get an HTML page (Blazor Server shell)
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("<html", content);
    }
}

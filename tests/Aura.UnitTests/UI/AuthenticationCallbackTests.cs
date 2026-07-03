using Aura.UI.Components.Auth;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.UI;

/// <summary>
/// Tests for the AuthenticationCallback component (query-parameter-based popup detection).
/// The component reads ?popup=true from the URL to determine popup context.
/// </summary>
public class AuthenticationCallbackTests : TestContext
{
    [Fact]
    public void CallbackPage_Renders_WithDataTestId()
    {
        // Arrange
        RegisterMinimalServices();

        // Act
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='auth-callback']"));
    }

    [Fact]
    public void CallbackPage_WithPopupQueryParam_ClosesWindow()
    {
        // Arrange — URL is set to authentication/callback?popup=true via custom NavigationManager
        var (_, invokedScripts) = RegisterForPopupTest(popup: true);

        // Act
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Wait for window.close() to be called
        cut.WaitForAssertion(() =>
        {
            Assert.Contains(invokedScripts, s => s.Contains("window.close()"));
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CallbackPage_WithPopupQueryParam_PostsAuthSuccess()
    {
        // Arrange — URL contains popup=true
        var (_, invokedScripts) = RegisterForPopupTest(popup: true);

        // Act
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Wait for postMessage to be called
        cut.WaitForAssertion(() =>
        {
            Assert.Contains(invokedScripts, s =>
                s.Contains("postMessage") && s.Contains("auth-success"));
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CallbackPage_WithoutPopupQuery_DoesNotCloseWindow()
    {
        // Arrange — URL does NOT contain popup=true (direct navigation)
        var (_, invokedScripts) = RegisterForPopupTest(popup: false);

        // Act
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Give OnAfterRenderAsync time to complete
        Thread.Sleep(500);

        // Assert — no JS calls were made (non-popup path uses NavigateTo, not JS)
        Assert.Empty(invokedScripts);
    }

    [Fact]
    public void CallbackPage_WhenPostMessageThrows_StillCloses()
    {
        // Arrange — postMessage script throws, close must still be called
        IJSRuntime mockJs = Substitute.For<IJSRuntime>();
        bool closeAttempted = false;

        mockJs
            .When(js => js.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                "eval", Arg.Any<object?[]?>()))
            .Do(callInfo =>
            {
                object?[]? args = callInfo.ArgAt<object?[]?>(1);
                if (args?.Length > 0 && args[0] is string s)
                {
                    if (s.Contains("postMessage"))
                        throw new JSException("Simulated postMessage failure");
                    if (s.Contains("window.close()"))
                        closeAttempted = true;
                }
            });

        RegisterForPopupTest(popup: true, jsOverride: mockJs);

        // Act
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Wait for close to be attempted despite postMessage failure
        cut.WaitForAssertion(() =>
        {
            Assert.True(closeAttempted);
        }, TimeSpan.FromSeconds(2));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private void RegisterMinimalServices()
    {
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<ILogger<AuthenticationCallback>>());
    }

    /// <summary>
    /// Registers services and returns the JS mock and invoked scripts list.
    /// The NavigationManager is set with a URI that either includes or excludes ?popup=true.
    /// </summary>
    private (IJSRuntime js, List<string> invokedScripts) RegisterForPopupTest(
        bool popup,
        IJSRuntime? jsOverride = null)
    {
        // Set up navigation URI via a custom NavigationManager
        string baseUrl = "http://localhost/";
        string path = popup
            ? "authentication/callback?popup=true"
            : "authentication/callback";
        var navManager = new UriNavigationManager(baseUrl + path);
        Services.AddSingleton<NavigationManager>(navManager);

        // Create JS mock if not overridden
        List<string> invokedScripts = [];
        IJSRuntime js = jsOverride ?? CreateJsMock(invokedScripts);
        Services.AddSingleton(js);
        Services.AddSingleton(Substitute.For<ILogger<AuthenticationCallback>>());

        return (js, invokedScripts);
    }

    private static IJSRuntime CreateJsMock(List<string> invokedScripts)
    {
        IJSRuntime mockJs = Substitute.For<IJSRuntime>();
        mockJs
            .When(js => js.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                "eval", Arg.Any<object?[]?>()))
            .Do(callInfo =>
            {
                object?[]? args = callInfo.ArgAt<object?[]?>(1);
                if (args?.Length > 0 && args[0] is string s)
                    invokedScripts.Add(s);
            });
        return mockJs;
    }

    /// <summary>
    /// NavigationManager that sets a fixed initial URI and does nothing on NavigateTo.
    /// Overrides NavigateToCore (protected virtual in .NET 9) instead of NavigateTo
    /// (which is a non-virtual public concrete method).
    /// </summary>
    private sealed class UriNavigationManager : NavigationManager
    {
        public UriNavigationManager(string uri)
        {
            Initialize("http://localhost/", uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            // No-op — no Blazor circuit in test context
        }
    }
}

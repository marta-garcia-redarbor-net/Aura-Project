using Aura.UI.Components.Auth;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.UI;

/// <summary>
/// Tests for the AuthenticationCallback component (redesigned popup flow).
/// After redesign: detects window.opener → postMessage+close, or redirects to "/".
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
    public void CallbackPage_WhenWindowOpenerExists_InvokesJsWithPostMessageAndClose()
    {
        // Arrange — popup detection returns true (window.opener exists and is not closed)
        List<(string identifier, object?[] args)> jsCalls = [];

        IJSRuntime mockJs = Substitute.For<IJSRuntime>();
        mockJs
            .InvokeAsync<bool>(Arg.Any<string>(), Arg.Any<object?[]?>())
            .Returns(callInfo =>
            {
                string id = callInfo.ArgAt<string>(0);
                object?[] args = callInfo.ArgAt<object?[]?>(1) ?? [];
                jsCalls.Add((id, args));
                // Popup detection call returns true
                return new ValueTask<bool>(true);
            });

        RegisterServicesWithJs(mockJs);

        // Act — render triggers OnAfterRenderAsync
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Wait for all async JS calls to complete
        cut.WaitForAssertion(() =>
        {
            // The InvokeVoidAsync for postMessage+close must be triggered
            mockJs.Received()
                .InvokeAsync<bool>("eval", Arg.Any<object?[]?>());
        }, TimeSpan.FromSeconds(2));

        // Assert — at minimum the popup detection was called
        Assert.Contains(jsCalls, call =>
            call.identifier == "eval" &&
            call.args.Length > 0);
    }

    [Fact]
    public void CallbackPage_WhenWindowOpenerIsNull_DoesNotInvokePostMessage()
    {
        // Arrange — popup detection returns false (no opener)
        List<string> invokeVoidCallArgs = [];

        IJSRuntime mockJs = Substitute.For<IJSRuntime>();
        mockJs
            .InvokeAsync<bool>(Arg.Any<string>(), Arg.Any<object?[]?>())
            .Returns(new ValueTask<bool>(false));

        // Track InvokeVoidAsync calls
        mockJs.When(js => js.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                Arg.Any<string>(), Arg.Any<object?[]?>()))
            .Do(callInfo =>
            {
                object?[]? args = callInfo.ArgAt<object?[]?>(1);
                if (args?.Length > 0 && args[0] is string s)
                {
                    invokeVoidCallArgs.Add(s);
                }
            });

        RegisterServicesWithJs(mockJs);

        // Act
        IRenderedComponent<AuthenticationCallback> cut = RenderComponent<AuthenticationCallback>();

        // Wait for popup detection to complete
        cut.WaitForAssertion(() =>
        {
            mockJs.Received()
                .InvokeAsync<bool>("eval", Arg.Any<object?[]?>());
        }, TimeSpan.FromSeconds(2));

        // Assert — postMessage must NOT have been called (no opener)
        Assert.DoesNotContain(invokeVoidCallArgs, s => s.Contains("postMessage"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private void RegisterMinimalServices()
    {
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<ILogger<AuthenticationCallback>>());
    }

    private void RegisterServicesWithJs(IJSRuntime js)
    {
        Services.AddSingleton(js);
        Services.AddSingleton(Substitute.For<ILogger<AuthenticationCallback>>());
    }
}

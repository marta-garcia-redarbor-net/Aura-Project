using System.Reflection;
using System.Text.Json;
using Aura.UI.Services;
using Bunit;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class AuthPopupServiceTests : TestContext
{
    [Fact]
    public void AuthPopupService_ShouldImplement_IAuthPopupService()
    {
        // Arrange & Act
        var service = new AuthPopupService();

        // Assert
        Assert.IsAssignableFrom<IAuthPopupService>(service);
    }

    [Fact]
    public void AuthPopupService_ShouldImplement_IAsyncDisposable()
    {
        // Arrange & Act
        var service = new AuthPopupService();

        // Assert
        Assert.IsAssignableFrom<IAsyncDisposable>(service);
    }

    [Fact]
    public async Task OpenMicrosoftLoginPopupAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new AuthPopupService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.OpenMicrosoftLoginPopupAsync("https://example.com/auth"));
        Assert.Contains("InitializeAsync", ex.Message);
    }

    [Fact]
    public async Task WaitForPopupResultAsync_WhenNotInitialized_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = new AuthPopupService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.WaitForPopupResultAsync(CancellationToken.None));
        Assert.Contains("InitializeAsync", ex.Message);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotInitialized_ShouldNotThrow()
    {
        // Arrange
        var service = new AuthPopupService();

        // Act & Assert — should not throw
        await service.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var service = new AuthPopupService();

        // Act & Assert — double disposal should be safe
        await service.DisposeAsync();
        await service.DisposeAsync();
    }

    [Fact]
    public async Task OpenMicrosoftLoginPopupAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var service = new AuthPopupService();
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.OpenMicrosoftLoginPopupAsync("https://example.com/auth"));
    }

    [Fact]
    public async Task WaitForPopupResultAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var service = new AuthPopupService();
        await service.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => service.WaitForPopupResultAsync(CancellationToken.None));
    }

    // --- PostMessage communication flow tests (triangulation) ---

    [Fact]
    public void ParseAuthResult_WithAuthSuccess_ReturnsSuccessResult()
    {
        // Arrange — simulates what postMessage sends: { type: "auth-success", token: "jwt..." }
        var element = JsonDocument.Parse("""{"type":"auth-success","token":"eyJhbGciOiJub25lIn0.abc"}""").RootElement;

        // Act — invoke ParseAuthResult via reflection (private static method)
        var method = typeof(AuthPopupService).GetMethod("ParseAuthResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = method!.Invoke(null, new object?[] { element }) as AuthResult;

        // Assert — postMessage with auth-success should produce a valid AuthResult
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("eyJhbGciOiJub25lIn0.abc", result.Token);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParseAuthResult_WithAuthError_ReturnsErrorResult()
    {
        // Arrange — simulates what postMessage sends: { type: "auth-error", error: "access_denied" }
        var element = JsonDocument.Parse("""{"type":"auth-error","error":"access_denied"}""").RootElement;

        // Act
        var method = typeof(AuthPopupService).GetMethod("ParseAuthResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = method!.Invoke(null, new object?[] { element }) as AuthResult;

        // Assert — postMessage with auth-error should produce a failed AuthResult
        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal("access_denied", result.Error);
    }

    [Fact]
    public void ParseAuthResult_WithNullInput_ReturnsNull()
    {
        // Arrange — simulates no postMessage received
        var method = typeof(AuthPopupService).GetMethod("ParseAuthResult",
            BindingFlags.NonPublic | BindingFlags.Static);

        // Act
        var result = method!.Invoke(null, new object?[] { null });

        // Assert — null postMessage data should return null AuthResult
        Assert.Null(result);
    }

    [Fact]
    public void ParseAuthResult_WithUnknownType_ReturnsNull()
    {
        // Arrange — simulates unexpected postMessage type
        var element = JsonDocument.Parse("""{"type":"unknown-event"}""").RootElement;

        // Act
        var method = typeof(AuthPopupService).GetMethod("ParseAuthResult",
            BindingFlags.NonPublic | BindingFlags.Static);
        var result = method!.Invoke(null, new object?[] { element });

        // Assert — unknown postMessage type should return null
        Assert.Null(result);
    }

}

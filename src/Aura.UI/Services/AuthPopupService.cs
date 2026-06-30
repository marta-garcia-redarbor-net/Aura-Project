using Microsoft.JSInterop;

namespace Aura.UI.Services;

/// <summary>
/// Manages the browser-popup-based OIDC authentication flow.
/// Communicates between the Blazor component and the popup window via JS interop.
/// </summary>
public sealed class AuthPopupService : IAuthPopupService, IAsyncDisposable
{
    private IJSObjectReference? _module;
    private bool _disposed;

    /// <inheritdoc />
    public async ValueTask InitializeAsync(IJSRuntime js)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/auth-popup.js");
    }

    /// <inheritdoc />
    public async Task OpenMicrosoftLoginPopupAsync(string authUrl)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_module is null)
            throw new InvalidOperationException("AuthPopupService has not been initialized. Call InitializeAsync first.");

        var popup = await _module.InvokeAsync<IJSObjectReference?>("openPopup", authUrl);
        if (popup is null)
            throw new InvalidOperationException("Popup was blocked by the browser. Allow popups for this site and try again.");
    }

    /// <inheritdoc />
    public async Task<AuthResult?> WaitForPopupResultAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_module is null)
            throw new InvalidOperationException("AuthPopupService has not been initialized. Call InitializeAsync first.");

        try
        {
            var result = await _module.InvokeAsync<object>("listenForAuthResult", 120000L);
            return ParseAuthResult(result);
        }
        catch (JSException)
        {
            // Timeout or JS interop failure — treat as no result
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch
            {
                // Best-effort cleanup — swallow disposal errors
            }

            _module = null;
        }
    }

    private static AuthResult? ParseAuthResult(object? rawResult)
    {
        if (rawResult is null)
            return null;

        // The JS module returns a dictionary-like object via JSObjectReference
        // We deserialize it via the IJSObjectReference pattern
        // For now, we handle the case where the result is a JsonElement
        if (rawResult is System.Text.Json.JsonElement element)
        {
            var type = element.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            var token = element.TryGetProperty("token", out var tokenProp) ? tokenProp.GetString() : null;
            var error = element.TryGetProperty("error", out var errorProp) ? errorProp.GetString() : null;

            return type switch
            {
                "auth-success" => new AuthResult(token ?? string.Empty, true, null),
                "auth-error" => new AuthResult(string.Empty, false, error),
                _ => null
            };
        }

        return null;
    }
}

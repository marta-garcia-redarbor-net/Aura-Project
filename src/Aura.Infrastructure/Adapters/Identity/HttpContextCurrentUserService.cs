using System.Security.Claims;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Maps the ASP.NET Core <see cref="ClaimsPrincipal"/> from the current HTTP context
/// to the domain-neutral <see cref="AuraUser"/> model.
/// Extracts Entra ID <c>oid</c> and <c>tid</c> claims when present.
/// Falls back to <c>preferred_username</c> when <c>name</c> or <c>email</c> claims are
/// absent from the access_token (common for v1.0 tokens issued to resource APIs).
/// </summary>
internal sealed partial class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpContextCurrentUserService> _logger;

    public HttpContextCurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<HttpContextCurrentUserService> logger)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public AuraUser? GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userId = principal.FindFirstValue(EntraIdClaims.ObjectId)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return null;

        var displayName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("preferred_username")
            ?? "";

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("preferred_username")
            ?? "";

        Log.UserResolved(_logger, userId, displayName, email);

        return new AuraUser
        {
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            Oid = principal.FindFirstValue(EntraIdClaims.ObjectId),
            TenantId = principal.FindFirstValue(EntraIdClaims.TenantId)
        };
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 5101,
            Level = LogLevel.Debug,
            Message = "Current user resolved: UserId={UserId}, DisplayName={DisplayName}, Email={Email}")]
        public static partial void UserResolved(ILogger logger, string userId, string displayName, string email);
    }
}

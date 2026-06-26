using System.Security.Claims;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Http;

namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Maps the ASP.NET Core <see cref="ClaimsPrincipal"/> from the current HTTP context
/// to the domain-neutral <see cref="AuraUser"/> model.
/// Extracts Entra ID <c>oid</c> and <c>tid</c> claims when present.
/// </summary>
internal sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public AuraUser? GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return null;

        return new AuraUser
        {
            UserId = userId,
            DisplayName = principal.FindFirstValue(ClaimTypes.Name) ?? "",
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? "",
            Oid = principal.FindFirstValue(EntraIdClaims.ObjectId),
            TenantId = principal.FindFirstValue(EntraIdClaims.TenantId)
        };
    }
}

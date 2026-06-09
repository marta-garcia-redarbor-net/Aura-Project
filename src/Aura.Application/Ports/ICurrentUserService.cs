using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for retrieving the current authenticated user context.
/// Implemented by infrastructure adapters — Application layer consumers
/// remain decoupled from the authentication mechanism.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Returns the current authenticated user, or <c>null</c> if unauthenticated.
    /// </summary>
    AuraUser? GetCurrentUser();
}

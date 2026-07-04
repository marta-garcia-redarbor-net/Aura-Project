using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IUserTriagePolicyProvider
{
    Task<UserTriagePolicy> GetApprovedPolicyAsync(string userId, CancellationToken ct);
}

using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

public sealed class DefaultUserTriagePolicyProvider : IUserTriagePolicyProvider
{
    public Task<UserTriagePolicy> GetApprovedPolicyAsync(string userId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return Task.FromResult(UserTriagePolicy.Empty);
    }
}

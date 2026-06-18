using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Identity;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.Dashboard;

internal sealed class MockJwtOptionsReadinessAdapter : IMockAuthReadinessProvider
{
    private readonly IOptionsMonitor<MockJwtOptions> _mockJwtOptions;

    public MockJwtOptionsReadinessAdapter(IOptionsMonitor<MockJwtOptions> mockJwtOptions)
    {
        ArgumentNullException.ThrowIfNull(mockJwtOptions);
        _mockJwtOptions = mockJwtOptions;
    }

    public bool IsConfigured()
    {
        var current = _mockJwtOptions.CurrentValue;

        return !string.IsNullOrWhiteSpace(current.Key)
               && !string.IsNullOrWhiteSpace(current.Issuer)
               && !string.IsNullOrWhiteSpace(current.Audience);
    }
}

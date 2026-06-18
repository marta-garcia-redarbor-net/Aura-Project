using Aura.Infrastructure.Adapters.Dashboard;
using Aura.Infrastructure.Adapters.Identity;
using Microsoft.Extensions.Options;

namespace Aura.UnitTests.Dashboard;

public class MockJwtOptionsReadinessAdapterTests
{
    [Fact]
    public void IsConfigured_WhenAllRequiredFieldsPresent_ReturnsTrue()
    {
        var options = new MockJwtOptions
        {
            Key = "valid-key-12345678901234567890123456789012",
            Issuer = "aura-test",
            Audience = "aura-api"
        };

        var adapter = new MockJwtOptionsReadinessAdapter(new StubOptionsMonitor(options));

        var result = adapter.IsConfigured();

        Assert.True(result);
    }

    [Fact]
    public void IsConfigured_WhenAnyRequiredFieldMissing_ReturnsFalse()
    {
        var options = new MockJwtOptions
        {
            Key = "",
            Issuer = "aura-test",
            Audience = "aura-api"
        };

        var adapter = new MockJwtOptionsReadinessAdapter(new StubOptionsMonitor(options));

        var result = adapter.IsConfigured();

        Assert.False(result);
    }

    private sealed class StubOptionsMonitor : IOptionsMonitor<MockJwtOptions>
    {
        public StubOptionsMonitor(MockJwtOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public MockJwtOptions CurrentValue { get; }

        public MockJwtOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<MockJwtOptions, string?> listener) => null;
    }
}

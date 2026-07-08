using Aura.Infrastructure.Adapters.Options;

namespace Aura.UnitTests.Demo;

public class DemoEndpointsTests
{
    [Fact]
    public void DemoModeOptions_EnabledDefaultsToFalse()
    {
        var options = new DemoModeOptions();
        Assert.False(options.Enabled);
    }

    [Fact]
    public void DemoModeOptions_EnabledCanBeSetToTrue()
    {
        var options = new DemoModeOptions { Enabled = true };
        Assert.True(options.Enabled);
    }

    [Fact]
    public void DemoModeOptions_HasExpectedSectionName()
    {
        Assert.Equal("DemoMode", DemoModeOptions.SectionName);
    }
}

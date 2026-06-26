using Aura.Workers;

namespace Aura.UnitTests.Workers;

public class ConnectorExecutionOptionsTests
{
    [Fact]
    public void DefaultPollingInterval_Is300Seconds()
    {
        var options = new ConnectorExecutionOptions();

        Assert.Equal(300, options.PollingIntervalSeconds);
    }
}

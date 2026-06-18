using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Dashboard;

namespace Aura.UnitTests.Dashboard;

public class AlwaysHealthyApiReadinessAdapterTests
{
    [Fact]
    public async Task GetReadinessAsync_ReturnsHealthy()
    {
        var adapter = new AlwaysHealthyApiReadinessAdapter();

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Healthy, result);
    }
}

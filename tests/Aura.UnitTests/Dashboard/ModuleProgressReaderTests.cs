using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class ModuleProgressReaderTests
{
    [Fact]
    public async Task GetAsync_WhenProviderReturnsEntries_PropagatesEntriesAndSeededFlag()
    {
        var provider = Substitute.For<IModuleProgressProvider>();
        var payload = new ModuleProgressDto(
            [new ModuleEntryDto("mod-a", ModuleProgressState.InProgress)],
            IsSeeded: true);

        provider.GetAsync(Arg.Any<CancellationToken>()).Returns(payload);

        var reader = new ModuleProgressReader(provider);

        var result = await reader.GetAsync(CancellationToken.None);

        Assert.True(result.IsSeeded);
        var entry = Assert.Single(result.Entries);
        Assert.Equal("mod-a", entry.ModuleId);
        Assert.Equal(ModuleProgressState.InProgress, entry.State);
    }

    [Fact]
    public async Task GetAsync_WhenProviderReturnsEmptyList_ReturnsEmptyList()
    {
        var provider = Substitute.For<IModuleProgressProvider>();
        provider.GetAsync(Arg.Any<CancellationToken>()).Returns(new ModuleProgressDto([], IsSeeded: true));

        var reader = new ModuleProgressReader(provider);

        var result = await reader.GetAsync(CancellationToken.None);

        Assert.Empty(result.Entries);
    }
}

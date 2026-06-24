using Aura.Application.Models;

namespace Aura.UnitTests.Sync;

public class SyncResultDtoTests
{
    [Fact]
    public void SyncResultDto_ConstructsWithResults()
    {
        var results = new List<SourceSyncResult>
        {
            new("teams", "success", 5, DateTimeOffset.UtcNow, null),
            new("outlook", "failure", 0, null, "auth_required")
        };

        var dto = new SyncResultDto(results);

        Assert.Equal(2, dto.Results.Count);
        Assert.Equal("teams", dto.Results[0].Source);
        Assert.Equal("success", dto.Results[0].Status);
        Assert.Equal(5, dto.Results[0].ItemCount);
        Assert.Null(dto.Results[0].FailureReason);
        Assert.Equal("outlook", dto.Results[1].Source);
        Assert.Equal("failure", dto.Results[1].Status);
        Assert.Equal("auth_required", dto.Results[1].FailureReason);
    }

    [Fact]
    public void SourceSyncState_ConstructsWithAllFields()
    {
        var ts = DateTimeOffset.UtcNow;
        var state = new SourceSyncState("teams", "success", 10, ts);

        Assert.Equal("teams", state.Source);
        Assert.Equal("success", state.Status);
        Assert.Equal(10, state.LastItemCount);
        Assert.Equal(ts, state.LastSyncTimestamp);
    }

    [Fact]
    public void SourceSyncState_NullTimestamp_IsAllowed()
    {
        var state = new SourceSyncState("outlook", "auth_required", 0, null);

        Assert.Null(state.LastSyncTimestamp);
        Assert.Equal(0, state.LastItemCount);
    }
}

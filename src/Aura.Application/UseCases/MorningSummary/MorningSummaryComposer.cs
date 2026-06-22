using Aura.Application.Models;
using Aura.Application.Ports;
using MorningSummaryModel = Aura.Application.Models.MorningSummary;
using Aura.Domain.WorkItems;

namespace Aura.Application.UseCases.MorningSummary;

public sealed class MorningSummaryComposer : IMorningSummaryComposer
{
    private readonly IWorkItemReader? _workItemReader;
    private readonly IMorningSummaryRankingPolicy _rankingPolicy;
    private readonly Func<DateTimeOffset> _utcNow;

    public MorningSummaryComposer(
        IMorningSummaryRankingPolicy rankingPolicy,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(rankingPolicy);

        _rankingPolicy = rankingPolicy;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public MorningSummaryComposer(
        IWorkItemReader workItemReader,
        IMorningSummaryRankingPolicy rankingPolicy,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(workItemReader);
        ArgumentNullException.ThrowIfNull(rankingPolicy);

        _workItemReader = workItemReader;
        _rankingPolicy = rankingPolicy;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<MorningSummaryModel> ComposeAsync(MorningSummaryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new MorningSummaryQuery(
            request.UserId,
            request.Window.ScheduledInstantUtc,
            request.Window.ScheduledInstantUtc.AddDays(1));

        var workItems = await ReadItemsAsync(query, cancellationToken);
        var entries = _rankingPolicy.Rank(workItems);

        return new MorningSummaryModel(
            request.UserId,
            request.Window,
            _utcNow(),
            entries);
    }

    private Task<IReadOnlyList<WorkItem>> ReadItemsAsync(MorningSummaryQuery query, CancellationToken cancellationToken)
    {
        if (_workItemReader is not null)
        {
            return _workItemReader.ReadForWindowAsync(query, cancellationToken);
        }

        return Task.FromResult<IReadOnlyList<WorkItem>>([]);
    }
}

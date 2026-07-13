using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Aura.Infrastructure.Adapters.Decisions;

/// <summary>
/// EF Core-backed store for interruption decisions (audit trail).
/// Uses the <c>InterruptionDecisions</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfInterruptionDecisionStore : IInterruptionDecisionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuraDbContext _db;

    public EfInterruptionDecisionStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        _db.InterruptionDecisions.Add(new Persistence.InterruptionDecision
        {
            Id = Guid.NewGuid().ToString(),
            WorkItemId = record.WorkItemId.ToString(),
            Title = record.Title,
            SourceType = record.SourceType,
            Decision = record.Decision,
            PriorityScore = record.PriorityScore,
            Explanation = record.Explanation,
            Timestamp = record.Timestamp.ToString("O"),
            FocusState = record.FocusState,
            RetrievedSemanticContext = SerializeContext(record.RetrievedSemanticContext),
            LlmRationale = record.LlmRationale,
            GuardrailOutcome = record.GuardrailOutcome,
            UserOid = record.UserOid
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(int page, int pageSize, string? userOid = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _db.InterruptionDecisions.AsNoTracking();

        if (!string.IsNullOrEmpty(userOid))
        {
            query = query.Where(e => e.UserOid == userOid);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(e => new InterruptionDecisionRecord(
                Guid.Parse(e.WorkItemId),
                e.Title,
                e.SourceType,
                e.Decision,
                e.PriorityScore,
                e.Explanation ?? string.Empty,
                DateTimeOffset.Parse(e.Timestamp),
                e.FocusState,
                DeserializeContext(e.RetrievedSemanticContext),
                e.LlmRationale,
                string.IsNullOrWhiteSpace(e.GuardrailOutcome) ? "confirmed" : e.GuardrailOutcome,
                e.UserOid))
            .ToList();

        return new PagedResult<InterruptionDecisionRecord>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _db.InterruptionDecisions.RemoveRange(_db.InterruptionDecisions);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializeContext(IReadOnlyList<DecisionContextItem>? context)
    {
        if (context is null || context.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(context, JsonOptions);
    }

    private static IReadOnlyList<DecisionContextItem> DeserializeContext(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<DecisionContextItem>>(value, JsonOptions) ?? [];
    }
}

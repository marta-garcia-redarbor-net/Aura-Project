using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.PrReview;

internal sealed partial class PrReviewConnectorAdapter : IConnectorAdapter
{
    private static readonly Func<IReadOnlyList<PrReviewDto>> DefaultFixtureProvider = LoadDefaultFixtures;

    private readonly ILogger<PrReviewConnectorAdapter> _logger;
    private readonly IWorkItemBuffer _buffer;
    private readonly PrReviewWorkItemMapper _mapper;
    private readonly Func<IReadOnlyList<PrReviewDto>> _fixtureProvider;
    private readonly IMessageSourceProvider<PrReviewDto>? _sourceProvider;

    public PrReviewConnectorAdapter(
        ILogger<PrReviewConnectorAdapter> logger,
        IWorkItemBuffer buffer,
        PrReviewWorkItemMapper mapper,
        Func<IReadOnlyList<PrReviewDto>>? fixtureProvider = null,
        IMessageSourceProvider<PrReviewDto>? sourceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(mapper);

        _logger = logger;
        _buffer = buffer;
        _mapper = mapper;
        _fixtureProvider = fixtureProvider ?? DefaultFixtureProvider;
        _sourceProvider = sourceProvider;
    }

    public string ConnectorName => "pr";

    public async Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<PrReviewDto> payloads;

        if (_sourceProvider is not null)
        {
            try
            {
                payloads = await _sourceProvider.FetchAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                Log.PrProviderHttpError(_logger, ex);
                return new ConnectorExecutionResult(
                    request.Identity, 0, ConnectorExecutionStatus.Failure,
                    $"Azure DevOps HTTP error: {ex.Message}");
            }
        }
        else
        {
            payloads = _fixtureProvider();
        }

        var mappedCount = 0;
        var skippedCount = 0;

        foreach (var payload in payloads)
        {
            if (_mapper.TryMap(payload, out var workItem) && workItem is not null)
            {
                if (_sourceProvider is null)
                {
                    ((IDictionary<string, string>)workItem.Metadata)[PrMetadataKeys.AttentionScope] = "direct";
                }

                _buffer.Enqueue(workItem);
                mappedCount++;
                continue;
            }

            skippedCount++;
            Log.PrSkipped(_logger, payload.PullRequestId);
        }

        var status = skippedCount > 0 ? ConnectorExecutionStatus.PartialFailure : ConnectorExecutionStatus.Success;
        var failureReason = skippedCount > 0 ? $"Skipped {skippedCount} invalid PR payload(s)." : null;

        Log.PrExecutionMapped(_logger, request.Identity.Source, request.Identity.Tenant, request.WindowStart, request.WindowEnd, mappedCount, skippedCount);

        return new ConnectorExecutionResult(
            request.Identity,
            mappedCount,
            status,
            failureReason,
            MaxProcessedAt: request.WindowEnd);
    }

    internal static IReadOnlyList<PrReviewDto> LoadDefaultFixtures() =>
    [
        new PrReviewDto
        {
            PullRequestId = 142,
            Title = "Hotfix: production crash on payment validation",
            RepoName = "Aura",
            Author = "Carlos Ruiz",
            CreatedAt = new DateTimeOffset(2026, 07, 1, 08, 30, 00, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 07, 1, 09, 15, 00, TimeSpan.Zero),
            Status = "active",
            Reviewers = ["Ana López", "Pedro Gómez"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity("ana-lopez-oid", "Ana López", false),
                new PrReviewerIdentity("pedro-gomez-oid", "Pedro Gómez", false)
            ],
            CommentCount = 12,
            FileCount = 3,
            SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/142",
            IsDraft = false,
            Priority = "critical"
        },
        new PrReviewDto
        {
            PullRequestId = 139,
            Title = "Fix: SSO redirect loop on token expiry",
            RepoName = "Aura.Auth",
            Author = "David Martínez",
            CreatedAt = new DateTimeOffset(2026, 07, 1, 07, 00, 00, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 07, 1, 10, 30, 00, TimeSpan.Zero),
            Status = "active",
            Reviewers = ["María García"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity("maria-garcia-oid", "María García", false)
            ],
            CommentCount = 8,
            FileCount = 5,
            SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura.Auth/pullrequest/139",
            IsDraft = false,
            Priority = "critical"
        },
        new PrReviewDto
        {
            PullRequestId = 145,
            Title = "Feature: Add reporting dashboard v2",
            RepoName = "Aura",
            Author = "Laura Sánchez",
            CreatedAt = new DateTimeOffset(2026, 07, 1, 11, 00, 00, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 07, 1, 14, 00, 00, TimeSpan.Zero),
            Status = "active",
            Reviewers = ["Ana López", "Carlos Ruiz", "Pedro Gómez"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity("ana-lopez-oid", "Ana López", false),
                new PrReviewerIdentity("platform-reviewers-oid", "Platform Reviewers", true),
                new PrReviewerIdentity("pedro-gomez-oid", "Pedro Gómez", false)
            ],
            CommentCount = 5,
            FileCount = 12,
            SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/145",
            IsDraft = false,
            Priority = "high"
        },
        new PrReviewDto
        {
            PullRequestId = 141,
            Title = "Refactor: Extract payment gateway adapter",
            RepoName = "Aura.Payments",
            Author = "Pedro Gómez",
            CreatedAt = new DateTimeOffset(2026, 06, 30, 16, 00, 00, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 07, 1, 12, 00, 00, TimeSpan.Zero),
            Status = "active",
            Reviewers = ["Laura Sánchez"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity("laura-sanchez-oid", "Laura Sánchez", false)
            ],
            CommentCount = 3,
            FileCount = 8,
            SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura.Payments/pullrequest/141",
            IsDraft = false,
            Priority = "high"
        },
        new PrReviewDto
        {
            PullRequestId = 148,
            Title = "Chore: Update dependency versions",
            RepoName = "Aura",
            Author = "Sistema",
            CreatedAt = new DateTimeOffset(2026, 07, 1, 06, 00, 00, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 07, 1, 06, 30, 00, TimeSpan.Zero),
            Status = "active",
            Reviewers = [],
            ReviewerIdentities = [],
            CommentCount = 0,
            FileCount = 15,
            SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/148",
            IsDraft = true,
            Priority = null
        },
        new PrReviewDto
        {
            PullRequestId = 150,
            Title = "Docs: Update API reference for v3 endpoints",
            RepoName = "Aura.Docs",
            Author = "María García",
            CreatedAt = new DateTimeOffset(2026, 07, 1, 09, 00, 00, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 07, 1, 09, 45, 00, TimeSpan.Zero),
            Status = "active",
            Reviewers = ["Carlos Ruiz"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity(null, "Carlos Ruiz", false)
            ],
            CommentCount = 1,
            FileCount = 4,
            SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura.Docs/pullrequest/150",
            IsDraft = false,
            Priority = "low"
        }
    ];

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3601,
            Level = LogLevel.Information,
            Message = "PR connector adapter executed for source {Source}, tenant {Tenant}, window {WindowStart} → {WindowEnd}, mapped {MappedCount}, skipped {SkippedCount}")]
        public static partial void PrExecutionMapped(
            ILogger logger,
            string source,
            string tenant,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            int mappedCount,
            int skippedCount);

        [LoggerMessage(
            EventId = 3602,
            Level = LogLevel.Warning,
            Message = "PR payload skipped because required fields were missing. PullRequestId={PullRequestId}")]
        public static partial void PrSkipped(
            ILogger logger,
            int pullRequestId);

        [LoggerMessage(
            EventId = 3603,
            Level = LogLevel.Error,
            Message = "Azure DevOps provider HTTP error")]
        public static partial void PrProviderHttpError(
            ILogger logger,
            HttpRequestException exception);
    }
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.PrReview;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.AzureDevOps;

/// <summary>
/// Fetches active pull requests from Azure DevOps REST API.
/// Falls back to fixtures when no PAT token is configured.
/// </summary>
internal sealed partial class AzureDevOpsPrProvider : IMessageSourceProvider<PrReviewDto>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDevOpsPrProvider> _logger;
    private readonly AzureDevOpsPrOptions _options;

    public AzureDevOpsPrProvider(
        HttpClient httpClient,
        ILogger<AzureDevOpsPrProvider> logger,
        AzureDevOpsPrOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _logger = logger;
        _options = options;
    }

    public async Task<IReadOnlyList<PrReviewDto>> FetchAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PatToken))
        {
            Log.AzureDevOpsDisabledOrNoToken(_logger, _options.Enabled);
            return LoadDefaultFixtures();
        }

        try
        {
            return await FetchFromAdoAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            Log.AzureDevOpsHttpError(_logger, _options.Organization, _options.Project, ex);
            throw;
        }
        catch (Exception ex) when (ex is not HttpRequestException and not OperationCanceledException)
        {
            Log.AzureDevOpsParseError(_logger, ex);
            return LoadDefaultFixtures();
        }
    }

    private async Task<IReadOnlyList<PrReviewDto>> FetchFromAdoAsync(CancellationToken ct)
    {
        var url = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_apis/git/pullrequests?searchCriteria.status=active&api-version=7.0";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{_options.PatToken}")));

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var adoResponse = await response.Content.ReadFromJsonAsync<AdoPullRequestResponse>(JsonOptions, ct);

        if (adoResponse?.Value is null || adoResponse.Value.Count == 0)
        {
            Log.AzureDevOpsNoResults(_logger, _options.Organization, _options.Project);
            return [];
        }

        var results = new List<PrReviewDto>(adoResponse.Value.Count);
        foreach (var pr in adoResponse.Value)
        {
            var dto = new PrReviewDto
            {
                PullRequestId = pr.PullRequestId,
                Title = pr.Title,
                RepoName = _options.Project,
                Author = pr.CreatedBy?.DisplayName ?? "Unknown",
                CreatedAt = pr.CreationDate,
                UpdatedAt = pr.CreationDate, // ADO doesn't return updatedAt in this endpoint
                Status = pr.Status ?? "active",
                Reviewers = pr.Reviewers?.Select(r => r.DisplayName ?? "Unknown").ToList() ?? [],
                ReviewerIdentities = pr.Reviewers?.Select(r =>
                    new PrReviewerIdentity(r.Id, r.DisplayName ?? "Unknown", r.IsContainer)).ToList() ?? [],
                CommentCount = 0,
                FileCount = 0,
                SourceLink = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_git/{_options.Project}/pullrequest/{pr.PullRequestId}",
                IsDraft = pr.IsDraft,
                Priority = ResolveAdoPriority(pr.IsDraft, pr.Reviewers)
            };

            results.Add(dto);
        }

        Log.AzureDevOpsFetched(_logger, results.Count, _options.Organization, _options.Project);
        return results;
    }

    private static string ResolveAdoPriority(bool isDraft, IReadOnlyList<AdoReviewer>? reviewers)
    {
        if (isDraft) return "low";
        if (reviewers?.Count >= 2) return "high";
        return "medium";
    }

    internal static IReadOnlyList<PrReviewDto> LoadDefaultFixtures() => PrReviewConnectorAdapter.LoadDefaultFixtures();

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed record AdoPullRequestResponse
    {
        public List<AdoPullRequest>? Value { get; init; }
    }

    private sealed record AdoPullRequest
    {
        public int PullRequestId { get; init; }
        public string? Title { get; init; }
        public DateTimeOffset? CreationDate { get; init; }
        public DateTimeOffset? ClosedDate { get; init; }
        public string? SourceRefName { get; init; }
        public string? TargetRefName { get; init; }
        public string? Status { get; init; }
        public AdoCreatedBy? CreatedBy { get; init; }
        public List<AdoReviewer>? Reviewers { get; init; }
        public bool IsDraft { get; init; }
    }

    private sealed record AdoCreatedBy
    {
        public string? DisplayName { get; init; }
    }

    private sealed record AdoReviewer
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public bool IsContainer { get; init; }
        public int Vote { get; init; }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3701, Level = LogLevel.Information,
            Message = "AzureDevOpsPrProvider fetched {Count} PRs from {Organization}/{Project}")]
        public static partial void AzureDevOpsFetched(ILogger logger, int count, string organization, string project);

        [LoggerMessage(EventId = 3702, Level = LogLevel.Information,
            Message = "AzureDevOpsPrProvider: disabled or no PAT token (Enabled={Enabled}). Using fixtures.")]
        public static partial void AzureDevOpsDisabledOrNoToken(ILogger logger, bool enabled);

        [LoggerMessage(EventId = 3703, Level = LogLevel.Warning,
            Message = "AzureDevOpsPrProvider: zero PRs returned from {Organization}/{Project}")]
        public static partial void AzureDevOpsNoResults(ILogger logger, string organization, string project);

        [LoggerMessage(EventId = 3704, Level = LogLevel.Error,
            Message = "AzureDevOpsPrProvider HTTP error for {Organization}/{Project}")]
        public static partial void AzureDevOpsHttpError(ILogger logger, string organization, string project, HttpRequestException ex);

        [LoggerMessage(EventId = 3705, Level = LogLevel.Warning,
            Message = "AzureDevOpsPrProvider: failed to parse response, falling back to fixtures")]
        public static partial void AzureDevOpsParseError(ILogger logger, Exception ex);
    }
}

/// <summary>
/// Options for the Azure DevOps PR provider.
/// </summary>
internal sealed class AzureDevOpsPrOptions
{
    public const string SectionName = "PrReview";

    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string PatToken { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

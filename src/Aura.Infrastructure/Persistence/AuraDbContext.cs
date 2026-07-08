using Aura.Domain.Calendar;
using Aura.Domain.WorkItems;
using Aura.Domain.FocusState;
using Aura.Application.Models;
using Aura.Domain.SemanticIndex.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence;

/// <summary>
/// Single <see cref="DbContext"/> for all Aura data stores in Azure SQL.
/// Entity configurations are applied via <see cref="IEntityTypeConfiguration{TEntity}"/>
/// classes in the <c>EntityConfigurations</c> namespace, registered in <see cref="OnModelCreating"/>.
/// </summary>
public class AuraDbContext : DbContext
{
    public AuraDbContext(DbContextOptions<AuraDbContext> options) : base(options)
    {
    }

    /// <summary>User-defined focus state overrides.</summary>
    public DbSet<FocusStateOverride> FocusStateOverrides => Set<FocusStateOverride>();

    /// <summary>Audit trail of interruption decisions.</summary>
    public DbSet<InterruptionDecision> InterruptionDecisions => Set<InterruptionDecision>();

    /// <summary>Alert rules (VIP senders and keywords).</summary>
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();

    /// <summary>Cross-process notification outbox entries.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Meeting alerts for calendar events.</summary>
    public DbSet<MeetingAlertEntity> MeetingAlerts => Set<MeetingAlertEntity>();

    /// <summary>Morning summary emission tracking.</summary>
    public DbSet<MorningSummaryEmission> MorningSummaryEmission => Set<MorningSummaryEmission>();

    /// <summary>Work items from all sources.</summary>
    public DbSet<WorkItemEntity> WorkItems => Set<WorkItemEntity>();

    /// <summary>Semantic index outbox entries.</summary>
    public DbSet<SemanticOutboxEntryEntity> SemanticOutbox => Set<SemanticOutboxEntryEntity>();

    /// <summary>MSAL token cache blobs.</summary>
    public DbSet<MsalTokenCacheEntry> MsalTokenCache => Set<MsalTokenCacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuraDbContext).Assembly);
    }
}

#region Entity types (used as EF Core shadow entities mapped to existing tables)

/// <summary>EF Core entity for the FocusStateOverrides table.</summary>
public class FocusStateOverride
{
    public string UserId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedAt { get; set; }
}

/// <summary>EF Core entity for the InterruptionDecisions table.</summary>
public class InterruptionDecision
{
    public string Id { get; set; } = string.Empty;
    public string WorkItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public int? PriorityScore { get; set; }
    public string? Explanation { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string FocusState { get; set; } = string.Empty;
}

/// <summary>EF Core entity for the AlertRules (VipSenders + AlertKeywords) tables.
/// Both tables share the same structure so we use a single entity with a discriminator.</summary>
public class AlertRule
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string AddedBy { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
}

/// <summary>EF Core entity for the NotificationOutbox table.</summary>
public class Notification
{
    public string Id { get; set; } = string.Empty;
    public string WorkItemId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public double Priority { get; set; }
    public string? TriggerRule { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? DispatchedAt { get; set; }
    public string? Explanation { get; set; }
    public string? Decision { get; set; }
    public string? TargetUserId { get; set; }
    public string? RuleResults { get; set; }
}

/// <summary>EF Core entity for the MeetingAlerts table.</summary>
public class MeetingAlertEntity
{
    public string EventId { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string LocalDate { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StartsAtUtc { get; set; } = string.Empty;
    public string? JoinUrl { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool HasBeenSent { get; set; }
}

/// <summary>EF Core entity for the MorningSummaryEmission table.</summary>
public class MorningSummaryEmission
{
    public string UserId { get; set; } = string.Empty;
    public string LocalDate { get; set; } = string.Empty;
    public string EmittedAt { get; set; } = string.Empty;
}

/// <summary>EF Core entity for the WorkItems table.</summary>
public class WorkItemEntity
{
    public string Id { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string CapturedAtUtc { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedAt { get; set; }
    public string? FaultReason { get; set; }
    public int? PriorityScore { get; set; }
    public string? OwnerUserId { get; set; }
}

/// <summary>EF Core entity for the SemanticOutbox table.</summary>
public class SemanticOutboxEntryEntity
{
    public string Id { get; set; } = string.Empty;
    public string CanonicalSourceId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Collection { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public string? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

/// <summary>EF Core entity for the MsalTokenCache table.</summary>
public class MsalTokenCacheEntry
{
    public string CacheKey { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public string UpdatedAt { get; set; } = string.Empty;
}

#endregion

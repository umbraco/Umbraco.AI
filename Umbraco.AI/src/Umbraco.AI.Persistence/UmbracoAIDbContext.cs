using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Persistence.Connections;
using Umbraco.AI.Persistence.Context;
using Umbraco.AI.Persistence.AuditLog;
using Umbraco.AI.Persistence.Profiles;
using Umbraco.AI.Persistence.Analytics;
using Umbraco.AI.Persistence.Analytics.Usage;
using Umbraco.AI.Persistence.Settings;
using Umbraco.AI.Persistence.Versioning;
using Umbraco.AI.Persistence.Tests;

namespace Umbraco.AI.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI persistence.
/// </summary>
public class UmbracoAIDbContext : DbContext
{
    /// <summary>
    /// AI provider connections.
    /// </summary>
    internal DbSet<AIConnectionEntity> Connections { get; set; } = null!;

    /// <summary>
    /// AI profile configurations.
    /// </summary>
    internal DbSet<AIProfileEntity> Profiles { get; set; } = null!;

    /// <summary>
    /// AI contexts containing resources.
    /// </summary>
    internal DbSet<AIContextEntity> Contexts { get; set; } = null!;

    /// <summary>
    /// AI context resources.
    /// </summary>
    internal DbSet<AIContextResourceEntity> ContextResources { get; set; } = null!;

    /// <summary>
    /// AI audit-log records.
    /// </summary>
    internal DbSet<AIAuditLogEntity> AuditLogs { get; set; } = null!;

    /// <summary>
    /// AI usage records (raw, ephemeral).
    /// </summary>
    internal DbSet<AIUsageRecordEntity> UsageRecords { get; set; } = null!;

    /// <summary>
    /// AI usage statistics (hourly aggregation).
    /// </summary>
    internal DbSet<AIUsageStatisticsHourlyEntity> UsageStatisticsHourly { get; set; } = null!;

    /// <summary>
    /// AI usage statistics (daily aggregation).
    /// </summary>
    internal DbSet<AIUsageStatisticsDailyEntity> UsageStatisticsDaily { get; set; } = null!;

    /// <summary>
    /// Unified entity version history.
    /// </summary>
    internal DbSet<AIEntityVersionEntity> EntityVersions { get; set; } = null!;

    /// <summary>
    /// AI settings (key-value store).
    /// </summary>
    internal DbSet<AISettingsEntity> Settings { get; set; } = null!;

    /// <summary>
    /// AI tests (test definitions).
    /// </summary>
    internal DbSet<AITestEntity> Tests { get; set; } = null!;

    /// <summary>
    /// AI test runs (execution results).
    /// </summary>
    internal DbSet<AITestRunEntity> TestRuns { get; set; } = null!;

    /// <summary>
    /// AI test transcripts (execution traces).
    /// </summary>
    internal DbSet<AITestTranscriptEntity> TestTranscripts { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of <see cref="UmbracoAIDbContext"/>.
    /// </summary>
    public UmbracoAIDbContext(DbContextOptions<UmbracoAIDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AIConnectionEntity>(entity =>
        {
            entity.ToTable("umbracoAIConnection");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ProviderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Settings);

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasIndex(e => e.ProviderId);
        });

        modelBuilder.Entity<AIProfileEntity>(entity =>
        {
            entity.ToTable("umbracoAIProfile");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Capability)
                .IsRequired();

            entity.Property(e => e.ProviderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ModelId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ConnectionId)
                .IsRequired();

            entity.Property(e => e.Settings);

            entity.Property(e => e.Tags)
                .HasMaxLength(2000);

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasIndex(e => e.Capability);

            entity.HasOne<AIConnectionEntity>()
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AIContextEntity>(entity =>
        {
            entity.ToTable("umbracoAIContext");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasMany(e => e.Resources)
                .WithOne(r => r.Context)
                .HasForeignKey(r => r.ContextId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AIContextResourceEntity>(entity =>
        {
            entity.ToTable("umbracoAIContextResource");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ContextId)
                .IsRequired();

            entity.Property(e => e.ResourceTypeId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.SortOrder)
                .IsRequired();

            entity.Property(e => e.Data)
                .IsRequired();

            entity.Property(e => e.InjectionMode)
                .IsRequired();

            entity.HasIndex(e => e.ContextId);
            entity.HasIndex(e => e.ResourceTypeId);
        });

        modelBuilder.Entity<AIAuditLogEntity>(entity =>
        {
            entity.ToTable("umbracoAIAuditLog");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.StartTime)
                .IsRequired();

            entity.Property(e => e.EndTime);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.ErrorCategory);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.UserId)
                .HasMaxLength(100);

            entity.Property(e => e.UserName)
                .HasMaxLength(255);

            entity.Property(e => e.EntityId)
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .HasMaxLength(50);

            entity.Property(e => e.Capability)
                .IsRequired();

            entity.Property(e => e.ProfileId)
                .IsRequired();

            entity.Property(e => e.ProfileAlias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ProviderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ModelId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.FeatureType)
                .HasMaxLength(50);

            entity.Property(e => e.FeatureId);

            entity.Property(e => e.ProfileVersion);

            entity.Property(e => e.FeatureVersion);

            entity.Property(e => e.InputTokens);
            entity.Property(e => e.OutputTokens);
            entity.Property(e => e.TotalTokens);

            entity.Property(e => e.PromptSnapshot);
            entity.Property(e => e.ResponseSnapshot);

            entity.Property(e => e.ParentAuditLogId);

            entity.Property(e => e.Metadata);

            // Indexes for query performance
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProfileId);
            entity.HasIndex(e => new { e.EntityId, e.EntityType });
            entity.HasIndex(e => new { e.StartTime, e.Status });
            entity.HasIndex(e => e.FeatureId);
            entity.HasIndex(e => new { e.FeatureType, e.FeatureId });
            entity.HasIndex(e => e.ParentAuditLogId);
        });

        modelBuilder.Entity<AIUsageRecordEntity>(entity =>
        {
            entity.ToTable("umbracoAIUsageRecord");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.Capability)
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasMaxLength(100);

            entity.Property(e => e.UserName)
                .HasMaxLength(255);

            entity.Property(e => e.ProfileId)
                .IsRequired();

            entity.Property(e => e.ProfileAlias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ProviderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ModelId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.FeatureType)
                .HasMaxLength(50);

            entity.Property(e => e.FeatureId);

            entity.Property(e => e.EntityId)
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .HasMaxLength(50);

            entity.Property(e => e.InputTokens)
                .IsRequired();

            entity.Property(e => e.OutputTokens)
                .IsRequired();

            entity.Property(e => e.TotalTokens)
                .IsRequired();

            entity.Property(e => e.DurationMs)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Indexes for query performance (critical for hourly aggregation queries)
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.Timestamp, e.Status });
        });

        modelBuilder.Entity<AIUsageStatisticsHourlyEntity>(entity =>
        {
            entity.ToTable("umbracoAIUsageStatisticsHourly");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Period)
                .IsRequired();

            entity.Property(e => e.ProviderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ModelId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ProfileId)
                .IsRequired();

            entity.Property(e => e.Capability)
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .HasMaxLength(50);

            entity.Property(e => e.FeatureType)
                .HasMaxLength(50);

            entity.Property(e => e.RequestCount)
                .IsRequired();

            entity.Property(e => e.SuccessCount)
                .IsRequired();

            entity.Property(e => e.FailureCount)
                .IsRequired();

            entity.Property(e => e.InputTokens)
                .IsRequired();

            entity.Property(e => e.OutputTokens)
                .IsRequired();

            entity.Property(e => e.TotalTokens)
                .IsRequired();

            entity.Property(e => e.TotalDurationMs)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Indexes for query performance
            entity.HasIndex(e => e.Period);
            entity.HasIndex(e => new { e.Period, e.ProviderId });
            entity.HasIndex(e => new { e.Period, e.ModelId });
            entity.HasIndex(e => new { e.Period, e.ProfileId });

            // Composite unique index for idempotent upserts
            entity.HasIndex(e => new { e.Period, e.ProviderId, e.ModelId, e.ProfileId, e.Capability, e.UserId, e.EntityType, e.FeatureType })
                .IsUnique();
        });

        modelBuilder.Entity<AIUsageStatisticsDailyEntity>(entity =>
        {
            entity.ToTable("umbracoAIUsageStatisticsDaily");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Period)
                .IsRequired();

            entity.Property(e => e.ProviderId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ModelId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ProfileId)
                .IsRequired();

            entity.Property(e => e.Capability)
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .HasMaxLength(50);

            entity.Property(e => e.FeatureType)
                .HasMaxLength(50);

            entity.Property(e => e.RequestCount)
                .IsRequired();

            entity.Property(e => e.SuccessCount)
                .IsRequired();

            entity.Property(e => e.FailureCount)
                .IsRequired();

            entity.Property(e => e.InputTokens)
                .IsRequired();

            entity.Property(e => e.OutputTokens)
                .IsRequired();

            entity.Property(e => e.TotalTokens)
                .IsRequired();

            entity.Property(e => e.TotalDurationMs)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Indexes for query performance
            entity.HasIndex(e => e.Period);
            entity.HasIndex(e => new { e.Period, e.ProviderId });
            entity.HasIndex(e => new { e.Period, e.ModelId });
            entity.HasIndex(e => new { e.Period, e.ProfileId });

            // Composite unique index for idempotent upserts
            entity.HasIndex(e => new { e.Period, e.ProviderId, e.ModelId, e.ProfileId, e.Capability, e.UserId, e.EntityType, e.FeatureType })
                .IsUnique();
        });

        modelBuilder.Entity<AIEntityVersionEntity>(entity =>
        {
            entity.ToTable("umbracoAIEntityVersion");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EntityId)
                .IsRequired();

            entity.Property(e => e.EntityType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.Snapshot)
                .IsRequired();

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.ChangeDescription)
                .HasMaxLength(500);

            // Composite unique index to ensure one version per entity per type
            entity.HasIndex(e => new { e.EntityId, e.EntityType, e.Version })
                .IsUnique();

            // Index for fast lookup by entity type and id
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        modelBuilder.Entity<AISettingsEntity>(entity =>
        {
            entity.ToTable("umbracoAISettings");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Value)
                .HasMaxLength(500);

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            // Unique constraint on Key to ensure only one value per setting
            entity.HasIndex(e => e.Key)
                .IsUnique();
        });

        modelBuilder.Entity<AITestEntity>(entity =>
        {
            entity.ToTable("umbracoAITest");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.TestTypeId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.TargetId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.TargetIsAlias)
                .IsRequired();

            entity.Property(e => e.TestCaseJson)
                .IsRequired();

            entity.Property(e => e.GradersJson);

            entity.Property(e => e.RunCount)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.Tags)
                .HasMaxLength(2000);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.BaselineRunId);

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            entity.Property(e => e.CreatedByUserId);
            entity.Property(e => e.ModifiedByUserId);

            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasIndex(e => e.TestTypeId);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<AITestRunEntity>(entity =>
        {
            entity.ToTable("umbracoAITestRun");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TestId)
                .IsRequired();

            entity.Property(e => e.TestVersion)
                .IsRequired();

            entity.Property(e => e.RunNumber)
                .IsRequired()
                .HasDefaultValue(1);

            entity.Property(e => e.ProfileId);

            entity.Property(e => e.ContextIds)
                .HasMaxLength(2000);

            entity.Property(e => e.ExecutedAt)
                .IsRequired();

            entity.Property(e => e.ExecutedByUserId);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.DurationMs)
                .IsRequired();

            entity.Property(e => e.TranscriptId);

            entity.Property(e => e.OutcomeType)
                .IsRequired();

            entity.Property(e => e.OutcomeValue);

            entity.Property(e => e.OutcomeFinishReason)
                .HasMaxLength(100);

            entity.Property(e => e.OutcomeTokenUsageJson);

            entity.Property(e => e.GraderResultsJson);

            entity.Property(e => e.MetadataJson);

            entity.Property(e => e.BatchId);

            entity.HasIndex(e => e.TestId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.BatchId);
            entity.HasIndex(e => new { e.TestId, e.ExecutedAt });
        });

        modelBuilder.Entity<AITestTranscriptEntity>(entity =>
        {
            entity.ToTable("umbracoAITestTranscript");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RunId)
                .IsRequired();

            entity.Property(e => e.MessagesJson);
            entity.Property(e => e.ToolCallsJson);
            entity.Property(e => e.ReasoningJson);
            entity.Property(e => e.TimingJson);

            entity.Property(e => e.FinalOutputJson)
                .IsRequired();

            entity.HasIndex(e => e.RunId)
                .IsUnique();
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Persistence.Connections;
using Umbraco.Ai.Persistence.Context;
using Umbraco.Ai.Persistence.AuditLog;
using Umbraco.Ai.Persistence.Profiles;
using Umbraco.Ai.Persistence.Analytics;
using Umbraco.Ai.Persistence.Analytics.Usage;
using Umbraco.Ai.Persistence.Settings;
using Umbraco.Ai.Persistence.Versioning;

namespace Umbraco.Ai.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI persistence.
/// </summary>
public class UmbracoAiDbContext : DbContext
{
    /// <summary>
    /// AI provider connections.
    /// </summary>
    internal DbSet<AiConnectionEntity> Connections { get; set; } = null!;

    /// <summary>
    /// AI profile configurations.
    /// </summary>
    internal DbSet<AiProfileEntity> Profiles { get; set; } = null!;

    /// <summary>
    /// AI contexts containing resources.
    /// </summary>
    internal DbSet<AiContextEntity> Contexts { get; set; } = null!;

    /// <summary>
    /// AI context resources.
    /// </summary>
    internal DbSet<AiContextResourceEntity> ContextResources { get; set; } = null!;

    /// <summary>
    /// AI audit-log records.
    /// </summary>
    internal DbSet<AiAuditLogEntity> AuditLogs { get; set; } = null!;

    /// <summary>
    /// AI usage records (raw, ephemeral).
    /// </summary>
    internal DbSet<AiUsageRecordEntity> UsageRecords { get; set; } = null!;

    /// <summary>
    /// AI usage statistics (hourly aggregation).
    /// </summary>
    internal DbSet<AiUsageStatisticsHourlyEntity> UsageStatisticsHourly { get; set; } = null!;

    /// <summary>
    /// AI usage statistics (daily aggregation).
    /// </summary>
    internal DbSet<AiUsageStatisticsDailyEntity> UsageStatisticsDaily { get; set; } = null!;

    /// <summary>
    /// Unified entity version history.
    /// </summary>
    internal DbSet<AiEntityVersionEntity> EntityVersions { get; set; } = null!;

    /// <summary>
    /// AI settings (key-value store).
    /// </summary>
    internal DbSet<AiSettingsEntity> Settings { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of <see cref="UmbracoAiDbContext"/>.
    /// </summary>
    public UmbracoAiDbContext(DbContextOptions<UmbracoAiDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AiConnectionEntity>(entity =>
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

        modelBuilder.Entity<AiProfileEntity>(entity =>
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

            entity.HasOne<AiConnectionEntity>()
                .WithMany()
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AiContextEntity>(entity =>
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

        modelBuilder.Entity<AiContextResourceEntity>(entity =>
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

        modelBuilder.Entity<AiAuditLogEntity>(entity =>
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

            entity.Property(e => e.DetailLevel)
                .IsRequired();

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

        modelBuilder.Entity<AiUsageRecordEntity>(entity =>
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

        modelBuilder.Entity<AiUsageStatisticsHourlyEntity>(entity =>
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

        modelBuilder.Entity<AiUsageStatisticsDailyEntity>(entity =>
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

        modelBuilder.Entity<AiEntityVersionEntity>(entity =>
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

        modelBuilder.Entity<AiSettingsEntity>(entity =>
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
    }
}

using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Persistence.Connections;
using Umbraco.Ai.Persistence.Context;
using Umbraco.Ai.Persistence.Governance;
using Umbraco.Ai.Persistence.Profiles;

namespace Umbraco.Ai.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI persistence.
/// </summary>
public class UmbracoAiDbContext : DbContext
{
    /// <summary>
    /// AI provider connections.
    /// </summary>
    public DbSet<AiConnectionEntity> Connections { get; set; } = null!;

    /// <summary>
    /// AI profile configurations.
    /// </summary>
    public DbSet<AiProfileEntity> Profiles { get; set; } = null!;

    /// <summary>
    /// AI contexts containing resources.
    /// </summary>
    public DbSet<AiContextEntity> Contexts { get; set; } = null!;

    /// <summary>
    /// AI context resources.
    /// </summary>
    public DbSet<AiContextResourceEntity> ContextResources { get; set; } = null!;

    /// <summary>
    /// AI governance traces.
    /// </summary>
    public DbSet<AiTraceEntity> Traces { get; set; } = null!;

    /// <summary>
    /// AI execution spans.
    /// </summary>
    public DbSet<AiExecutionSpanEntity> ExecutionSpans { get; set; } = null!;

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
            entity.ToTable("umbracoAiConnection");
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

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasIndex(e => e.ProviderId);
        });

        modelBuilder.Entity<AiProfileEntity>(entity =>
        {
            entity.ToTable("umbracoAiProfile");
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
            entity.ToTable("umbracoAiContext");
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

            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasMany(e => e.Resources)
                .WithOne(r => r.Context)
                .HasForeignKey(r => r.ContextId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiContextResourceEntity>(entity =>
        {
            entity.ToTable("umbracoAiContextResource");
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

        modelBuilder.Entity<AiTraceEntity>(entity =>
        {
            entity.ToTable("umbracoAiTrace");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TraceId)
                .HasMaxLength(32)
                .IsRequired();

            entity.Property(e => e.SpanId)
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(e => e.StartTime)
                .IsRequired();

            entity.Property(e => e.EndTime);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.ErrorCategory);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.UserName)
                .HasMaxLength(255);

            entity.Property(e => e.EntityId)
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .HasMaxLength(50);

            entity.Property(e => e.OperationType)
                .HasMaxLength(100)
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

            entity.Property(e => e.InputTokens);
            entity.Property(e => e.OutputTokens);
            entity.Property(e => e.TotalTokens);

            entity.Property(e => e.PromptSnapshot);
            entity.Property(e => e.ResponseSnapshot);

            entity.Property(e => e.DetailLevel)
                .IsRequired();

            // Indexes for query performance
            entity.HasIndex(e => e.TraceId);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProfileId);
            entity.HasIndex(e => new { e.EntityId, e.EntityType });
            entity.HasIndex(e => new { e.StartTime, e.Status });

            // Relationship to execution spans
            entity.HasMany(e => e.Spans)
                .WithOne(s => s.Trace)
                .HasForeignKey(s => s.TraceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiExecutionSpanEntity>(entity =>
        {
            entity.ToTable("umbracoAiExecutionSpan");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TraceId)
                .IsRequired();

            entity.Property(e => e.SpanId)
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(e => e.ParentSpanId)
                .HasMaxLength(16);

            entity.Property(e => e.SpanName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.SpanType)
                .IsRequired();

            entity.Property(e => e.SequenceNumber)
                .IsRequired();

            entity.Property(e => e.StartTime)
                .IsRequired();

            entity.Property(e => e.EndTime);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.InputData);
            entity.Property(e => e.OutputData);
            entity.Property(e => e.ErrorData);

            entity.Property(e => e.RetryCount);
            entity.Property(e => e.TokensUsed);

            // Indexes
            entity.HasIndex(e => e.TraceId);
            entity.HasIndex(e => e.SpanId);
            entity.HasIndex(e => new { e.TraceId, e.SequenceNumber });
        });
    }
}

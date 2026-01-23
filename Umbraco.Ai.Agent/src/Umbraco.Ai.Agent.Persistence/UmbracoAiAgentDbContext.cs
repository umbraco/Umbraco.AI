using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Agent.Persistence.Agents;

namespace Umbraco.Ai.Agent.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI AiAgent persistence.
/// </summary>
public class UmbracoAiAgentDbContext : DbContext
{
    /// <summary>
    /// Agents table.
    /// </summary>
    internal DbSet<AiAgentEntity> Agents { get; set; } = null!;

    /// <summary>
    /// Agent version history.
    /// </summary>
    internal DbSet<AiAgentVersionEntity> AgentVersions { get; set; } = null!;

    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    public UmbracoAiAgentDbContext(DbContextOptions<UmbracoAiAgentDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AiAgentEntity>(entity =>
        {
            entity.ToTable("UmbracoAiAgent");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.ProfileId)
                .IsRequired(false);

            entity.Property(e => e.ContextIds)
                .HasMaxLength(4000);

            entity.Property(e => e.Instructions);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            // Indexes
            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasIndex(e => e.ProfileId);
        });

        modelBuilder.Entity<AiAgentVersionEntity>(entity =>
        {
            entity.ToTable("umbracoAiAgentVersion");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AgentId)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.Snapshot)
                .IsRequired();

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.ChangeDescription)
                .HasMaxLength(500);

            // Foreign key with cascade delete (when agent is deleted, delete its versions)
            entity.HasOne<AiAgentEntity>()
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to ensure one version per agent
            entity.HasIndex(e => new { e.AgentId, e.Version })
                .IsUnique();

            entity.HasIndex(e => e.AgentId);
        });
    }
}

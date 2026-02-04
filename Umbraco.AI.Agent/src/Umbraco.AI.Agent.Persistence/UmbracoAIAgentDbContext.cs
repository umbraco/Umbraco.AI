using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Persistence.Agents;

namespace Umbraco.AI.Agent.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI AIAgent persistence.
/// </summary>
public class UmbracoAIAgentDbContext : DbContext
{
    /// <summary>
    /// Agents table.
    /// </summary>
    internal DbSet<AIAgentEntity> Agents { get; set; } = null!;

    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    public UmbracoAIAgentDbContext(DbContextOptions<UmbracoAIAgentDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AIAgentEntity>(entity =>
        {
            entity.ToTable("umbracoAIAgent");
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

            entity.Property(e => e.ScopeIds)
                .HasMaxLength(2000);

            entity.Property(e => e.EnabledToolIds)
                .HasMaxLength(4000);

            entity.Property(e => e.EnabledToolScopeIds)
                .HasMaxLength(2000);

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
    }
}

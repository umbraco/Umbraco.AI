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
    public DbSet<AiAgentEntity> Agents { get; set; } = null!;

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

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.ProfileId);

            entity.Property(e => e.Tags)
                .HasMaxLength(2000);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.Scope)
                .HasMaxLength(4000);

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.DateModified)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.Alias)
                .IsUnique();

            entity.HasIndex(e => e.ProfileId);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Umbraco.Ai.Prompt.Persistence.Prompts;

namespace Umbraco.Ai.Prompt.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI AiPrompt persistence.
/// </summary>
public class UmbracoAiPromptDbContext : DbContext
{
    /// <summary>
    /// Prompts table.
    /// </summary>
    public DbSet<AiPromptEntity> Prompts { get; set; } = null!;

    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    public UmbracoAiPromptDbContext(DbContextOptions<UmbracoAiPromptDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AiPromptEntity>(entity =>
        {
            entity.ToTable("umbracoAiPrompt");
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

            entity.Property(e => e.TagsJson)
                .HasMaxLength(2000);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.ScopeJson)
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

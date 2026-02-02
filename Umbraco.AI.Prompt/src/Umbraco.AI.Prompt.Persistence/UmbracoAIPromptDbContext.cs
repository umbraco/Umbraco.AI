using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Prompt.Persistence.Prompts;

namespace Umbraco.AI.Prompt.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI AIPrompt persistence.
/// </summary>
public class UmbracoAIPromptDbContext : DbContext
{
    /// <summary>
    /// Prompts table.
    /// </summary>
    internal DbSet<AIPromptEntity> Prompts { get; set; } = null!;

    /// <summary>
    /// Creates a new instance of the DbContext.
    /// </summary>
    public UmbracoAIPromptDbContext(DbContextOptions<UmbracoAIPromptDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AIPromptEntity>(entity =>
        {
            entity.ToTable("umbracoAIPrompt");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Alias)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Instructions)
                .IsRequired();

            entity.Property(e => e.ProfileId);

            entity.Property(e => e.ContextIds)
                .HasMaxLength(4000);

            entity.Property(e => e.Tags)
                .HasMaxLength(2000);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.IncludeEntityContext)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.Scope)
                .HasMaxLength(4000);

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

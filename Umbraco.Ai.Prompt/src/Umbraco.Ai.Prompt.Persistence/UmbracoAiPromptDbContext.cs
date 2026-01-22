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
    internal DbSet<AiPromptEntity> Prompts { get; set; } = null!;

    /// <summary>
    /// Prompt version history.
    /// </summary>
    internal DbSet<AiPromptVersionEntity> PromptVersions { get; set; } = null!;

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

        modelBuilder.Entity<AiPromptVersionEntity>(entity =>
        {
            entity.ToTable("umbracoAiPromptVersion");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PromptId)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.Snapshot)
                .IsRequired();

            entity.Property(e => e.DateCreated)
                .IsRequired();

            entity.Property(e => e.ChangeDescription)
                .HasMaxLength(500);

            // Foreign key with cascade delete (when prompt is deleted, delete its versions)
            entity.HasOne<AiPromptEntity>()
                .WithMany()
                .HasForeignKey(e => e.PromptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique index to ensure one version per prompt
            entity.HasIndex(e => new { e.PromptId, e.Version })
                .IsUnique();

            entity.HasIndex(e => e.PromptId);
        });
    }
}

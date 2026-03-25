using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.Sqlite.VectorStore;

namespace Umbraco.AI.Search.Sqlite;

/// <summary>
/// EF Core DbContext for Umbraco AI Search persistence on SQLite.
/// </summary>
public class UmbracoAISearchDbContext : DbContext
{
    /// <summary>
    /// Vector store entries.
    /// </summary>
    internal DbSet<AIVectorEntryEntity> VectorEntries { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of <see cref="UmbracoAISearchDbContext"/>.
    /// </summary>
    public UmbracoAISearchDbContext(DbContextOptions<UmbracoAISearchDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AIVectorEntryEntity>(entity =>
        {
            entity.ToTable("umbracoAISearchVectorEntry");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IndexName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.DocumentId)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Culture)
                .HasMaxLength(10);

            entity.Property(e => e.ChunkIndex)
                .IsRequired();

            entity.Property(e => e.Vector)
                .IsRequired();

            entity.Property(e => e.Metadata);

            // Composite unique index: one chunk per document/culture per index
            entity.HasIndex(e => new { e.IndexName, e.DocumentId, e.Culture, e.ChunkIndex })
                .IsUnique();

            // Index for fast lookups by document+culture (delete all chunks for a document variant)
            entity.HasIndex(e => new { e.IndexName, e.DocumentId, e.Culture });

            // Index for fast lookups by index name (reset, count, search)
            entity.HasIndex(e => e.IndexName);
        });
    }
}

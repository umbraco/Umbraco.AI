using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.Db.VectorStore;
using Umbraco.Cms.Core;

namespace Umbraco.AI.Search.Db;

/// <summary>
/// EF Core DbContext for Umbraco AI Search vector store persistence.
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

    /// <summary>
    /// The custom migrations history table name for Umbraco AI Search.
    /// </summary>
    internal const string MigrationsHistoryTableName = "__UmbracoAISearchMigrationsHistory";

    /// <summary>
    /// The migration name prefix used to identify Umbraco AI Search migrations.
    /// </summary>
    internal const string MigrationPrefix = "UmbracoAISearch_";

    /// <summary>
    /// Configures the EF Core database provider with the correct migrations assembly.
    /// </summary>
    internal static void ConfigureProvider(
        DbContextOptionsBuilder options,
        string? connectionString,
        string? providerName)
    {
        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(providerName))
        {
            return;
        }

        switch (providerName)
        {
            case Constants.ProviderNames.SQLServer:
                options.UseSqlServer(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Search.Db.SqlServer");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Search.Db.Sqlite");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.AI.Search.");
        }
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

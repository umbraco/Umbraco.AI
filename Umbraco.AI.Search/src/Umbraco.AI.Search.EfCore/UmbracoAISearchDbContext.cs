using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Search.EfCore.VectorStore;
using Umbraco.Cms.Core;

namespace Umbraco.AI.Search.EfCore;

/// <summary>
/// EF Core DbContext for Umbraco AI Search persistence.
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
                    x.MigrationsAssembly("Umbraco.AI.Search.SqlServer"));
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                    x.MigrationsAssembly("Umbraco.AI.Search.Sqlite"));
                break;

            default:
                throw new InvalidOperationException(
                    $"The database provider '{providerName}' is not supported by Umbraco.AI.Search.EfCore. " +
                    $"Supported providers: SQL Server, SQLite.");
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

            entity.Property(e => e.Vector)
                .IsRequired();

            entity.Property(e => e.Metadata);

            // Composite unique index: one document per index
            entity.HasIndex(e => new { e.IndexName, e.DocumentId })
                .IsUnique();

            // Index for fast lookups by index name (reset, count, search)
            entity.HasIndex(e => e.IndexName);
        });
    }
}

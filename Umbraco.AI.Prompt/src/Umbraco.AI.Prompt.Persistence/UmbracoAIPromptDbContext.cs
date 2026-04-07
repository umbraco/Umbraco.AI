using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Prompt.Persistence.Prompts;
using Umbraco.Cms.Core;

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

    /// <summary>
    /// The custom migrations history table name for Umbraco AI Prompt.
    /// </summary>
    internal const string MigrationsHistoryTableName = "__UmbracoAIPromptMigrationsHistory";

    /// <summary>
    /// The migration name prefix used to identify Umbraco AI Prompt migrations.
    /// </summary>
    internal const string MigrationPrefix = "UmbracoAIPrompt_";

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
                    x.MigrationsAssembly("Umbraco.AI.Prompt.Persistence.SqlServer");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Prompt.Persistence.Sqlite");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.AI.Prompt.");
        }
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

            entity.Property(e => e.GuardrailIds)
                .HasMaxLength(4000);

            entity.Property(e => e.Tags)
                .HasMaxLength(2000);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.IncludeEntityContext)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.DisplayMode)
                .IsRequired()
                .HasDefaultValue(0);

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

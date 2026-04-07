using Microsoft.EntityFrameworkCore;
using Umbraco.AI.Agent.Persistence.Agents;
using Umbraco.AI.Core.Configuration;
using Umbraco.Cms.Core;

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

    /// <summary>
    /// The shared migrations history table name for all Umbraco AI packages.
    /// </summary>
    internal const string MigrationsHistoryTableName = AIConnectionStringResolver.MigrationsHistoryTableName;

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
                    x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.SqlServer");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            case Constants.ProviderNames.SQLLite:
            case "Microsoft.Data.SQLite":
                options.UseSqlite(connectionString, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.Sqlite");
                    x.MigrationsHistoryTable(MigrationsHistoryTableName);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Database provider '{providerName}' is not supported by Umbraco.AI.Agent.");
        }
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

            entity.Property(e => e.AgentType)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.Config);

            entity.Property(e => e.ProfileId)
                .IsRequired(false);

            entity.Property(e => e.GuardrailIds)
                .HasMaxLength(4000);

            entity.Property(e => e.SurfaceIds)
                .HasMaxLength(2000);

            entity.Property(e => e.Scope);

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

            entity.HasIndex(e => e.AgentType);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

using Umbraco.AI.Search.Db;

namespace Umbraco.AI.Search.Db.Sqlite;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAISearchDbContext"/> for EF Core migrations.
/// </summary>
public class UmbracoAISearchDbContextFactory : IDesignTimeDbContextFactory<UmbracoAISearchDbContext>
{
    /// <inheritdoc />
    public UmbracoAISearchDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAISearchDbContext>();

        // Use a dummy connection string for design-time operations
        optionsBuilder.UseSqlite(
            "Data Source=:memory:",
            x =>
            {
                x.MigrationsAssembly(typeof(UmbracoAISearchDbContextFactory).Assembly.FullName);
                x.MigrationsHistoryTable(UmbracoAISearchDbContext.MigrationsHistoryTableName);
            });

        return new UmbracoAISearchDbContext(optionsBuilder.Options);
    }
}

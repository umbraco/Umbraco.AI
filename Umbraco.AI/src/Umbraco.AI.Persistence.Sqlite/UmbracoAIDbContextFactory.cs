using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.AI.Persistence.Sqlite;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAIDbContext"/> for EF Core migrations.
/// </summary>
public class UmbracoAIDbContextFactory : IDesignTimeDbContextFactory<UmbracoAIDbContext>
{
    /// <inheritdoc />
    public UmbracoAIDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIDbContext>();

        // Use a dummy connection string for design-time operations
        optionsBuilder.UseSqlite(
            "Data Source=:memory:",
            x => x.MigrationsAssembly(typeof(UmbracoAIDbContextFactory).Assembly.FullName));

        return new UmbracoAIDbContext(optionsBuilder.Options);
    }
}

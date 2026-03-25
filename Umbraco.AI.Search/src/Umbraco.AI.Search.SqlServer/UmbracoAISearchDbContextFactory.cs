using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Umbraco.AI.Search.EFCore;

namespace Umbraco.AI.Search.SqlServer;

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
        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAISearch_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAISearchDbContextFactory).Assembly.FullName));

        return new UmbracoAISearchDbContext(optionsBuilder.Options);
    }
}

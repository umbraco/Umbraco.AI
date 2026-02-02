using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.AI.Persistence.SqlServer;

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
        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAi_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAIDbContextFactory).Assembly.FullName));

        return new UmbracoAIDbContext(optionsBuilder.Options);
    }
}

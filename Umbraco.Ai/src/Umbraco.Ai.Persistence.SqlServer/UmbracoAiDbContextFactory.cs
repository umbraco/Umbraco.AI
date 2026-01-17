using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Ai.Persistence.SqlServer;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAiDbContext"/> for EF Core migrations.
/// </summary>
public class UmbracoAiDbContextFactory : IDesignTimeDbContextFactory<UmbracoAiDbContext>
{
    /// <inheritdoc />
    public UmbracoAiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAiDbContext>();

        // Use a dummy connection string for design-time operations
        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAi_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAiDbContextFactory).Assembly.FullName));

        return new UmbracoAiDbContext(optionsBuilder.Options);
    }
}

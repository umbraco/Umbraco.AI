using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.AI.Agent.Persistence.SqlServer;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAIAgentDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAIAgentDbContextFactory : IDesignTimeDbContextFactory<UmbracoAIAgentDbContext>
{
    /// <inheritdoc />
    public UmbracoAIAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIAgentDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAIAgent_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAIAgentDbContextFactory).Assembly.FullName));

        return new UmbracoAIAgentDbContext(optionsBuilder.Options);
    }
}

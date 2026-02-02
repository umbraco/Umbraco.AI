using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Ai.Agent.Persistence.SqlServer;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAiAgentDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAiAgentDbContextFactory : IDesignTimeDbContextFactory<UmbracoAiAgentDbContext>
{
    /// <inheritdoc />
    public UmbracoAiAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAiAgentDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAiAgent_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAiAgentDbContextFactory).Assembly.FullName));

        return new UmbracoAiAgentDbContext(optionsBuilder.Options);
    }
}

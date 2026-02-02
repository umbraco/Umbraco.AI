using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.AI.Agent.Persistence.Sqlite;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAIAgentDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAIAgentDbContextFactory : IDesignTimeDbContextFactory<UmbracoAIAgentDbContext>
{
    /// <inheritdoc />
    public UmbracoAIAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIAgentDbContext>();

        optionsBuilder.UseSqlite(
            "Data Source=UmbracoAIAgent_Design.db",
            x => x.MigrationsAssembly(typeof(UmbracoAIAgentDbContextFactory).Assembly.FullName));

        return new UmbracoAIAgentDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Ai.Agent.Persistence.Sqlite;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAiAgentDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAiAgentDbContextFactory : IDesignTimeDbContextFactory<UmbracoAiAgentDbContext>
{
    /// <inheritdoc />
    public UmbracoAiAgentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAiAgentDbContext>();

        optionsBuilder.UseSqlite(
            "Data Source=UmbracoAiAgent_Design.db",
            x => x.MigrationsAssembly(typeof(UmbracoAiAgentDbContextFactory).Assembly.FullName));

        return new UmbracoAiAgentDbContext(optionsBuilder.Options);
    }
}

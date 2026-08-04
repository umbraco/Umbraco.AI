using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Umbraco.AI.Agent.Conversations.Persistence;

namespace Umbraco.AI.Agent.Conversations.Persistence.SqlServer;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAIConversationsDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAIConversationsDbContextFactory : IDesignTimeDbContextFactory<UmbracoAIConversationsDbContext>
{
    /// <inheritdoc />
    public UmbracoAIConversationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIConversationsDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAIConversations_Design;Integrated Security=true;TrustServerCertificate=true",
            x =>
            {
                x.MigrationsAssembly(typeof(UmbracoAIConversationsDbContextFactory).Assembly.FullName);
                x.MigrationsHistoryTable(UmbracoAIConversationsDbContext.MigrationsHistoryTableName);
            });

        return new UmbracoAIConversationsDbContext(optionsBuilder.Options);
    }
}

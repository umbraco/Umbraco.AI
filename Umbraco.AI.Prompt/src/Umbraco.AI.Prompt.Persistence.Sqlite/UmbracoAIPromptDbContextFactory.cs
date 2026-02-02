using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.AI.Prompt.Persistence.Sqlite;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAIPromptDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAIPromptDbContextFactory : IDesignTimeDbContextFactory<UmbracoAIPromptDbContext>
{
    /// <inheritdoc />
    public UmbracoAIPromptDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>();

        optionsBuilder.UseSqlite(
            "Data Source=UmbracoAIPrompt_Design.db",
            x => x.MigrationsAssembly(typeof(UmbracoAIPromptDbContextFactory).Assembly.FullName));

        return new UmbracoAIPromptDbContext(optionsBuilder.Options);
    }
}

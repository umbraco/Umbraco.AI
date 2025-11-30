using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Ai.Prompt.Persistence.Sqlite;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAiPromptDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAiPromptDbContextFactory : IDesignTimeDbContextFactory<UmbracoAiPromptDbContext>
{
    /// <inheritdoc />
    public UmbracoAiPromptDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAiPromptDbContext>();

        optionsBuilder.UseSqlite(
            "Data Source=UmbracoAiPrompt_Design.db",
            x => x.MigrationsAssembly(typeof(UmbracoAiPromptDbContextFactory).Assembly.FullName));

        return new UmbracoAiPromptDbContext(optionsBuilder.Options);
    }
}

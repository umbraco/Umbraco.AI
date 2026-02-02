using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.AI.Prompt.Persistence.SqlServer;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAIPromptDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAIPromptDbContextFactory : IDesignTimeDbContextFactory<UmbracoAIPromptDbContext>
{
    /// <inheritdoc />
    public UmbracoAIPromptDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAIPrompt_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAIPromptDbContextFactory).Assembly.FullName));

        return new UmbracoAIPromptDbContext(optionsBuilder.Options);
    }
}

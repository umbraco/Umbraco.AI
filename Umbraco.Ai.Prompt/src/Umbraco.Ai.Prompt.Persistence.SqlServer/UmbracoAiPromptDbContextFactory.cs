using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbraco.Ai.Prompt.Persistence.SqlServer;

/// <summary>
/// Design-time factory for creating <see cref="UmbracoAiPromptDbContext"/> for EF Core CLI tools.
/// </summary>
public class UmbracoAiPromptDbContextFactory : IDesignTimeDbContextFactory<UmbracoAiPromptDbContext>
{
    /// <inheritdoc />
    public UmbracoAiPromptDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAiPromptDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=.;Database=UmbracoAiPrompt_Design;Integrated Security=true;TrustServerCertificate=true",
            x => x.MigrationsAssembly(typeof(UmbracoAiPromptDbContextFactory).Assembly.FullName));

        return new UmbracoAiPromptDbContext(optionsBuilder.Options);
    }
}

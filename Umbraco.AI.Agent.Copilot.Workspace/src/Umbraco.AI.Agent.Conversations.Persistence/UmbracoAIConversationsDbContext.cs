using Microsoft.EntityFrameworkCore;

namespace Umbraco.AI.Agent.Conversations.Persistence;

/// <summary>
/// EF Core DbContext for Umbraco AI Conversations (conversations, messages, projects, project resources).
/// </summary>
/// <remarks>
/// Entity sets, factories, and the shared <c>__UmbracoAIMigrationsHistory</c> history-table wiring are
/// added in Phase 2. Kept intentionally empty at scaffold time so the assembly compiles.
/// </remarks>
public class UmbracoAIConversationsDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIConversationsDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public UmbracoAIConversationsDbContext(DbContextOptions<UmbracoAIConversationsDbContext> options)
        : base(options)
    {
    }
}

using Microsoft.EntityFrameworkCore;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration;

/// <summary>
/// A trivial, schema-free <see cref="DbContext"/> used only to obtain a real
/// <c>IEFCoreScopeProvider{DummyConversationDbContext}</c> wired through Umbraco CMS's own
/// EF Core scoping machinery (see <see cref="AmbientScopeRaceTests"/> for why this exists).
/// It never needs to read/write real data - the regression it proves is about ambient
/// scope-stack concurrency, not persistence - so no entities/tables are declared.
/// </summary>
public sealed class DummyConversationDbContext : DbContext
{
    public DummyConversationDbContext(DbContextOptions<DummyConversationDbContext> options)
        : base(options)
    {
    }
}

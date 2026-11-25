using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence;
using Umbraco.AI.Agent.Persistence.Agents;
using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Core.Versioning;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Persistence;

/// <summary>
/// An agent's allowed user groups survive a save and reload against the real SQLite migrations,
/// rows that existed before the column read as "everyone", and a version rollback restores the list.
/// </summary>
public class AllowedUserGroupIdsPersistenceTests : IDisposable
{
    private const string PreviousMigration = "20260916124151_UmbracoAIAgent_AddStarterPrompts";

    private readonly SqliteConnection _connection;

    public AllowedUserGroupIdsPersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task SaveAndReload_KeepsTheAllowedUserGroupIds()
    {
        var groupA = Guid.NewGuid();
        var groupB = Guid.NewGuid();
        var agent = NewAgent();
        agent.AllowedUserGroupIds = [groupA, groupB];

        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
            db.Agents.Add(AIAgentEntityFactory.BuildEntity(agent));
            await db.SaveChangesAsync();
        }

        await using var reload = CreateContext();
        var entity = await reload.Agents.AsNoTracking().SingleAsync(e => e.Id == agent.Id);
        var reloaded = AIAgentEntityFactory.BuildDomain(entity);

        reloaded.AllowedUserGroupIds.ShouldBe([groupA, groupB]);
    }

    [Fact]
    public async Task Save_StoresTheKeysInLowercaseDashedFormat()
    {
        var group = Guid.NewGuid();
        var agent = NewAgent();
        agent.AllowedUserGroupIds = [group];

        var entity = AIAgentEntityFactory.BuildEntity(agent);

        entity.AllowedUserGroupIds.ShouldBe($"[\"{group:D}\"]");
    }

    [Fact]
    public async Task UpdateAndReload_ClearingTheListReadsBackAsEmpty()
    {
        var agent = NewAgent();
        agent.AllowedUserGroupIds = [Guid.NewGuid()];

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var entity = AIAgentEntityFactory.BuildEntity(agent);
        db.Agents.Add(entity);
        await db.SaveChangesAsync();

        agent.AllowedUserGroupIds = [];
        AIAgentEntityFactory.UpdateEntity(entity, agent);
        await db.SaveChangesAsync();

        await using var reload = CreateContext();
        var reloaded = AIAgentEntityFactory.BuildDomain(
            await reload.Agents.AsNoTracking().SingleAsync(e => e.Id == agent.Id));
        reloaded.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistingRowsFromBeforeTheColumn_ReadAsEmpty()
    {
        var agentId = Guid.NewGuid();
        await using (var db = CreateContext())
        {
            // Stop one migration short of the new column, then add a row the way an older install has it.
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO umbracoAIAgent (Id, Alias, Name, AgentType, IsActive, DateCreated, DateModified, Version)
                VALUES ({0}, 'legacy', 'Legacy', 0, 1, '2026-01-01 00:00:00', '2026-01-01 00:00:00', 1)
                """,
                agentId);
            await db.Database.MigrateAsync();
        }

        await using var reload = CreateContext();
        var entity = await reload.Agents.AsNoTracking().SingleAsync(e => e.Id == agentId);

        AIAgentEntityFactory.BuildDomain(entity).AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void Rollback_RestoresAnEmptyListFromASnapshotTakenWithNoLimit()
    {
        var adapter = NewAdapter();
        var agent = NewAgent();

        var restored = (AIAgent)adapter.RestoreFromSnapshot(adapter.CreateSnapshot(agent))!;

        restored.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void Rollback_RestoresTheListFromTheSnapshot()
    {
        var adapter = NewAdapter();
        var agent = NewAgent();
        var groups = new[] { Guid.NewGuid(), Guid.NewGuid() };
        agent.AllowedUserGroupIds = groups;

        var restored = (AIAgent)adapter.RestoreFromSnapshot(adapter.CreateSnapshot(agent))!;

        restored.AllowedUserGroupIds.ShouldBe(groups);
    }

    [Fact]
    public void Rollback_ToASnapshotFromBeforeTheFeature_RestoresAnEmptyList()
    {
        var adapter = NewAdapter();
        var snapshot = adapter.CreateSnapshot(NewAgent());
        var withoutProperty = System.Text.Json.Nodes.JsonNode.Parse(snapshot)!.AsObject();
        withoutProperty.Remove("allowedUserGroupIds").ShouldBeTrue();

        var restored = (AIAgent)adapter.RestoreFromSnapshot(withoutProperty.ToJsonString())!;

        restored.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void VersionDiff_ReportsAChangeToTheAllowedUserGroupIds()
    {
        var adapter = NewAdapter();
        var from = NewAgent();
        var to = NewAgent();
        to.AllowedUserGroupIds = [Guid.NewGuid()];

        var changes = adapter.CompareVersions(from, to);

        changes.ShouldContain(c => c.Path == "AllowedUserGroupIds");
    }

    private UmbracoAIAgentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UmbracoAIAgentDbContext>()
            .UseSqlite(_connection, x =>
            {
                x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.Sqlite");
                x.MigrationsHistoryTable(AIConnectionStringResolver.MigrationsHistoryTableName);
            })
            .Options;

        return new UmbracoAIAgentDbContext(options);
    }

    private static IAIVersionableEntityAdapter NewAdapter() =>
        new AIAgentVersionableEntityAdapter(Mock.Of<IAIAgentService>(), Mock.Of<IAIEntityVersionService>());

    private static AIAgent NewAgent() => new()
    {
        Id = Guid.NewGuid(),
        Alias = "probe",
        Name = "Probe",
        ProfileId = Guid.NewGuid(),
        Config = new AIStandardAgentConfig()
    };
}

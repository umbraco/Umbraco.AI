using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Moq;

using Shouldly;

using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Persistence;
using Umbraco.AI.Prompt.Persistence.Prompts;

using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Persistence;

/// <summary>
/// A prompt's allowed user groups survive a save and reload against the real SQLite migrations,
/// rows that existed before the column read as "everyone", and a version rollback restores the list.
/// </summary>
public class AllowedUserGroupIdsPersistenceTests : IDisposable
{
    private const string PreviousMigration = "20260315100000_UmbracoAIPrompt_AddGuardrailIds";

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
        var prompt = NewPrompt();
        prompt.AllowedUserGroupIds = [groupA, groupB];

        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
            db.Prompts.Add(AIPromptEntityFactory.BuildEntity(prompt));
            await db.SaveChangesAsync();
        }

        await using var reload = CreateContext();
        var entity = await reload.Prompts.AsNoTracking().SingleAsync(e => e.Id == prompt.Id);

        AIPromptEntityFactory.BuildDomain(entity).AllowedUserGroupIds.ShouldBe([groupA, groupB]);
    }

    [Fact]
    public void Save_StoresTheKeysInLowercaseDashedFormat()
    {
        var group = Guid.NewGuid();
        var prompt = NewPrompt();
        prompt.AllowedUserGroupIds = [group];

        var entity = AIPromptEntityFactory.BuildEntity(prompt);

        entity.AllowedUserGroupIds.ShouldBe($"[\"{group:D}\"]");
    }

    [Fact]
    public void Save_StoresNullWhenTheListIsEmpty()
    {
        AIPromptEntityFactory.BuildEntity(NewPrompt()).AllowedUserGroupIds.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAndReload_ClearingTheListReadsBackAsEmpty()
    {
        var prompt = NewPrompt();
        prompt.AllowedUserGroupIds = [Guid.NewGuid()];

        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        var entity = AIPromptEntityFactory.BuildEntity(prompt);
        db.Prompts.Add(entity);
        await db.SaveChangesAsync();

        prompt.AllowedUserGroupIds = [];
        AIPromptEntityFactory.UpdateEntity(entity, prompt);
        await db.SaveChangesAsync();

        await using var reload = CreateContext();
        var reloaded = AIPromptEntityFactory.BuildDomain(
            await reload.Prompts.AsNoTracking().SingleAsync(e => e.Id == prompt.Id));
        reloaded.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistingRowsFromBeforeTheColumn_ReadAsEmpty()
    {
        var promptId = Guid.NewGuid();
        await using (var db = CreateContext())
        {
            // Stop one migration short of the new column, then add a row the way an older install has it.
            await db.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO umbracoAIPrompt (Id, Alias, Name, Instructions, IsActive, IncludeEntityContext, OptionCount, DisplayMode, DateCreated, DateModified, Version)
                VALUES ({0}, 'legacy', 'Legacy', 'Do it', 1, 1, 1, 0, '2026-01-01 00:00:00', '2026-01-01 00:00:00', 1)
                """,
                promptId);
            await db.Database.MigrateAsync();
        }

        await using var reload = CreateContext();
        var entity = await reload.Prompts.AsNoTracking().SingleAsync(e => e.Id == promptId);

        AIPromptEntityFactory.BuildDomain(entity).AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void Rollback_RestoresAnEmptyListFromASnapshotTakenWithNoLimit()
    {
        var adapter = NewAdapter();

        var restored = (AIPrompt)adapter.RestoreFromSnapshot(adapter.CreateSnapshot(NewPrompt()))!;

        restored.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void Rollback_RestoresTheListFromTheSnapshot()
    {
        var adapter = NewAdapter();
        var prompt = NewPrompt();
        var groups = new[] { Guid.NewGuid(), Guid.NewGuid() };
        prompt.AllowedUserGroupIds = groups;

        var restored = (AIPrompt)adapter.RestoreFromSnapshot(adapter.CreateSnapshot(prompt))!;

        restored.AllowedUserGroupIds.ShouldBe(groups);
    }

    [Fact]
    public void Rollback_ToASnapshotFromBeforeTheFeature_RestoresAnEmptyList()
    {
        var adapter = NewAdapter();
        var snapshot = adapter.CreateSnapshot(NewPrompt());
        var withoutProperty = System.Text.Json.Nodes.JsonNode.Parse(snapshot)!.AsObject();
        withoutProperty.Remove("allowedUserGroupIds").ShouldBeTrue();

        var restored = (AIPrompt)adapter.RestoreFromSnapshot(withoutProperty.ToJsonString())!;

        restored.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public void VersionDiff_ReportsAChangeToTheAllowedUserGroupIds()
    {
        var adapter = NewAdapter();
        var from = NewPrompt();
        var to = NewPrompt();
        to.AllowedUserGroupIds = [Guid.NewGuid()];

        var changes = adapter.CompareVersions(from, to);

        changes.ShouldContain(c => c.Path == "AllowedUserGroupIds");
    }

    private UmbracoAIPromptDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>()
            .UseSqlite(_connection, x =>
            {
                x.MigrationsAssembly("Umbraco.AI.Prompt.Persistence.Sqlite");
                x.MigrationsHistoryTable(AIConnectionStringResolver.MigrationsHistoryTableName);
            })
            .Options;

        return new UmbracoAIPromptDbContext(options);
    }

    private static IAIVersionableEntityAdapter NewAdapter() =>
        new AIPromptVersionableEntityAdapter(Mock.Of<IAIPromptService>(), Mock.Of<IAIEntityVersionService>());

    private static AIPrompt NewPrompt() => new()
    {
        Id = Guid.NewGuid(),
        Alias = "probe",
        Name = "Probe",
        Instructions = "Do it"
    };
}

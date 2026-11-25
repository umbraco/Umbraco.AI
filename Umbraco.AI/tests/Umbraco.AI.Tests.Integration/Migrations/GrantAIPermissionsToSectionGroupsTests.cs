// GP-6 (AC1, AC2, AC5, AC6, AC7)

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NPoco;

using Umbraco.AI.Core;
using Umbraco.AI.Core.Migrations;
using Umbraco.AI.Core.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Persistence.Sqlite.Services;

namespace Umbraco.AI.Tests.Integration.Migrations;

/// <summary>
/// The core upgrade grants the 9 core verbs to AI section groups and the admin group, once, without undoing an admin's choice.
/// </summary>
/// <remarks>
/// Runs the real migration against a real SQLite database holding the CMS user group tables.
/// </remarks>
public sealed class GrantAIPermissionsToSectionGroupsTests : IDisposable
{
    private static readonly string[] CoreVerbs =
    [
        AIPermissions.ManageConnections,
        AIPermissions.ManageProfiles,
        AIPermissions.ManageContexts,
        AIPermissions.ManageKnowledgeSets,
        AIPermissions.ManageGuardrails,
        AIPermissions.ManageSettings,
        AIPermissions.ViewAuditLogs,
        AIPermissions.ViewAnalytics,
        AIPermissions.ManageTests,
    ];

    private readonly SqliteConnection _keepAlive;
    private readonly UmbracoDatabase _database;
    private readonly IMigrationContext _context;
    private readonly AppCaches _appCaches = AppCaches.Create(Mock.Of<IRequestCache>());
    private int _nextGroupId = 100;

    public GrantAIPermissionsToSectionGroupsTests()
    {
        // A shared in-memory database lives while one connection holds it open.
        var connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAlive = new SqliteConnection(connectionString);
        _keepAlive.Open();

        var syntax = new SqliteSyntaxProvider(Options.Create(new GlobalSettings()), NullLogger<SqliteSyntaxProvider>.Instance);
        var sqlContext = new SqlContext(syntax, DatabaseType.SQLite, new Mock<IPocoDataFactory>().Object);
        _database = new UmbracoDatabase(
            connectionString,
            sqlContext,
            SqliteFactory.Instance,
            NullLogger<UmbracoDatabase>.Instance,
            null,
            null!);

        _database.Execute("CREATE TABLE umbracoUserGroup (id INTEGER PRIMARY KEY, \"key\" TEXT NOT NULL)");
        _database.Execute("CREATE TABLE umbracoUserGroup2App (userGroupId INTEGER NOT NULL, app TEXT NOT NULL, PRIMARY KEY (userGroupId, app))");
        _database.Execute("CREATE TABLE umbracoUserGroup2Permission (id INTEGER PRIMARY KEY AUTOINCREMENT, userGroupKey TEXT NOT NULL, permission TEXT NOT NULL)");

        var context = new Mock<IMigrationContext>();
        context.SetupGet(x => x.Database).Returns(_database);
        context.SetupGet(x => x.SqlContext).Returns(sqlContext);
        context.SetupGet(x => x.Logger).Returns(NullLogger<IMigrationContext>.Instance);
        _context = context.Object;
    }

    public void Dispose()
    {
        _database.Dispose();
        _keepAlive.Dispose();
    }

    #region Happy path

    [Fact]
    public async Task Migration_GivesAISectionGroup_AllNineCoreVerbs()
    {
        // Given a group with the AI section before the upgrade
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);

        // When  the Umbraco.AI migration runs
        await RunMigrationAsync();

        // Then  the group holds all 9 core verbs (AC1)
        PermissionsOf(group).ShouldBe(CoreVerbs, ignoreOrder: true);
    }

    [Fact]
    public async Task Migration_GivesAdminGroup_AllNineCoreVerbs()
    {
        // Given the admin group's AI section row added in the same upgrade
        var admin = AddGroup(Cms.Core.Constants.Security.AdminGroupKey, Constants.Sections.AI);

        // When  the Umbraco.AI migration runs
        await RunMigrationAsync();

        // Then  the admin group holds all 9 core verbs (AC2)
        PermissionsOf(admin).ShouldBe(CoreVerbs, ignoreOrder: true);
    }

    [Fact]
    public async Task Migration_GivesAdminGroup_AllNineCoreVerbs_EvenWithoutAISectionRow()
    {
        // Given an admin group whose AI section row is not there yet
        var admin = AddGroup(Cms.Core.Constants.Security.AdminGroupKey);

        // When  the Umbraco.AI migration runs
        await RunMigrationAsync();

        // Then  the admin group still holds all 9 core verbs (AC2)
        PermissionsOf(admin).ShouldBe(CoreVerbs, ignoreOrder: true);
    }

    [Fact]
    public async Task Migration_KeepsAGroupsOtherPermissions()
    {
        // Given a group with the AI section and a CMS permission already
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);
        AddPermission(group, "F");

        // When  the Umbraco.AI migration runs
        await RunMigrationAsync();

        // Then  it keeps the CMS permission and gains the 9 core verbs
        PermissionsOf(group).ShouldBe([.. CoreVerbs, "F"], ignoreOrder: true);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task ReRun_KeepsTurnedOffVerbOff()
    {
        // Given the migration ran and an admin turned off Uai.Connection.Manage on a group
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);
        await RunMigrationAsync();
        _database.Execute(
            "DELETE FROM umbracoUserGroup2Permission WHERE userGroupKey = @0 AND permission = @1",
            group,
            AIPermissions.ManageConnections);

        // When  the grant step runs again
        await RunMigrationAsync();

        // Then  the group still lacks Uai.Connection.Manage (AC5)
        PermissionsOf(group).ShouldNotContain(AIPermissions.ManageConnections);
    }

    [Fact]
    public async Task ReRun_AddsNoDuplicateRows()
    {
        // Given the migration already ran
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);
        await RunMigrationAsync();

        // When  the grant step runs again
        await RunMigrationAsync();

        // Then  each group holds each AI verb at most once (AC6)
        PermissionsOf(group).Count.ShouldBe(CoreVerbs.Length);
    }

    [Fact]
    public async Task MigrationDoesNotGrantToGroupGivenSectionAfterItRan()
    {
        // Given a group given the AI section after the migration ran
        var group = AddGroup(Guid.NewGuid());
        await RunMigrationAsync();
        _database.Execute("INSERT INTO umbracoUserGroup2App (userGroupId, app) VALUES (@0, @1)", GroupId(group), Constants.Sections.AI);

        // When  nothing re-runs the migration
        // Then  the migration did not grant it any AI verbs (AC7)
        PermissionsOf(group).ShouldBeEmpty();
    }

    [Fact]
    public async Task ClearsCachedUserGroupsAndUsers_ButNotTheRuntimeCache()
    {
        // Given cached user groups, users and an unrelated runtime cache entry
        var groupCache = _appCaches.IsolatedCaches.GetOrCreate<IUserGroup>();
        var userCache = _appCaches.IsolatedCaches.GetOrCreate<IUser>();
        groupCache.Insert("group", () => new object());
        userCache.Insert("user", () => new object());
        _appCaches.RuntimeCache.Insert("other", () => new object());

        // When  the grant step runs
        await RunMigrationAsync();

        // Then  the group and user caches are empty and the runtime cache is untouched
        groupCache.Get("group").ShouldBeNull();
        userCache.Get("user").ShouldBeNull();
        _appCaches.RuntimeCache.Get("other").ShouldNotBeNull();
    }

    #endregion

    private async Task RunMigrationAsync() => await new GrantAIPermissionsToSectionGroups(_context, _appCaches).RunAsync();

    private Guid AddGroup(Guid key, string? app = null)
    {
        var id = _nextGroupId++;
        _database.Execute("INSERT INTO umbracoUserGroup (id, \"key\") VALUES (@0, @1)", id, key);
        if (app is not null)
        {
            _database.Execute("INSERT INTO umbracoUserGroup2App (userGroupId, app) VALUES (@0, @1)", id, app);
        }

        return key;
    }

    private int GroupId(Guid key) =>
        _database.ExecuteScalar<int>("SELECT id FROM umbracoUserGroup WHERE \"key\" = @0", key);

    private void AddPermission(Guid group, string permission) =>
        _database.Execute("INSERT INTO umbracoUserGroup2Permission (userGroupKey, permission) VALUES (@0, @1)", group, permission);

    private List<string> PermissionsOf(Guid group) =>
        _database.Fetch<string>("SELECT permission FROM umbracoUserGroup2Permission WHERE userGroupKey = @0", group);
}

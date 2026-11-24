// GP-6 (AC4)

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using NPoco;

using Shouldly;

using Umbraco.AI.Core;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Prompt.Core.Migrations;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Persistence.Sqlite.Services;

using Xunit;

namespace Umbraco.AI.Prompt.Tests.Integration.Migrations;

/// <summary>
/// The prompt upgrade grants Uai.Prompt.Manage to AI section groups and the admin group, once, without undoing an admin's choice.
/// </summary>
/// <remarks>
/// Runs the real migration against a real SQLite database holding the CMS user group tables.
/// </remarks>
public sealed class GrantAIPromptPermissionsTests : IDisposable
{
    private readonly SqliteConnection _keepAlive;
    private readonly UmbracoDatabase _database;
    private readonly IMigrationContext _context;
    private readonly AppCaches _appCaches = AppCaches.Create(Mock.Of<IRequestCache>());
    private int _nextGroupId = 100;

    public GrantAIPromptPermissionsTests()
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

    [Fact]
    public async Task Migration_GivesAISectionGroup_ThePromptVerbOnce()
    {
        // Given a group with the AI section before the upgrade
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);

        // When  the Umbraco.AI.Prompt migration runs
        await RunMigrationAsync();

        // Then  the group holds Uai.Prompt.Manage exactly once (AC4)
        PermissionsOf(group).ShouldBe([AIPermissions.ManagePrompts]);
    }

    [Fact]
    public async Task Migration_GivesAdminGroup_ThePromptVerb_EvenWithoutAISectionRow()
    {
        // Given an admin group whose AI section row is not there yet
        var admin = AddGroup(Cms.Core.Constants.Security.AdminGroupKey);

        // When  the Umbraco.AI.Prompt migration runs
        await RunMigrationAsync();

        // Then  the admin group holds Uai.Prompt.Manage (AC4)
        PermissionsOf(admin).ShouldBe([AIPermissions.ManagePrompts]);
    }

    [Fact]
    public async Task Migration_LeavesAGroupWithoutTheAISectionAlone()
    {
        // Given a group without the AI section
        var group = AddGroup(Guid.NewGuid());

        // When  the Umbraco.AI.Prompt migration runs
        await RunMigrationAsync();

        // Then  it gets nothing
        PermissionsOf(group).ShouldBeEmpty();
    }

    [Fact]
    public async Task Migration_KeepsAGroupsOtherPermissions()
    {
        // Given a group with the AI section, a CMS permission and a core AI verb
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);
        AddPermission(group, "F");
        AddPermission(group, AIPermissions.ManageProfiles);

        // When  the Umbraco.AI.Prompt migration runs
        await RunMigrationAsync();

        // Then  it keeps those and gains the prompt verb
        PermissionsOf(group).ShouldBe(["F", AIPermissions.ManageProfiles, AIPermissions.ManagePrompts], ignoreOrder: true);
    }

    [Fact]
    public async Task ReRun_AddsNoDuplicateRows()
    {
        // Given the migration already ran
        var group = AddGroup(Guid.NewGuid(), Constants.Sections.AI);
        await RunMigrationAsync();

        // When  the grant step runs again
        await RunMigrationAsync();

        // Then  the group holds the prompt verb once (AC4)
        PermissionsOf(group).Count.ShouldBe(1);
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

    private async Task RunMigrationAsync() => await new GrantAIPromptPermissions(_context, _appCaches).RunAsync();

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

    private void AddPermission(Guid group, string permission) =>
        _database.Execute("INSERT INTO umbracoUserGroup2Permission (userGroupKey, permission) VALUES (@0, @1)", group, permission);

    private List<string> PermissionsOf(Guid group) =>
        _database.Fetch<string>("SELECT permission FROM umbracoUserGroup2Permission WHERE userGroupKey = @0", group);
}

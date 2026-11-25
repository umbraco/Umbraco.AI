using System.Data.Common;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence;
using Umbraco.AI.Agent.Persistence.Agents;
using Umbraco.AI.Agent.Tests.Integration.Fixtures;
using Umbraco.AI.Core.Configuration;

using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Persistence;

/// <summary>
/// The agent list queries keep only the agents a set of user groups may use, in the same SQL query, against
/// the real SQLite migrations.
/// </summary>
public class AgentUserGroupListFilterTests : IDisposable
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();
    private static readonly Guid DeletedGroup = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly CommandRecorder _commands = new();
    private readonly EFCoreAIAgentRepository _repository;

    public AgentUserGroupListFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var db = CreateContext(withRecorder: false))
        {
            db.Database.Migrate();
        }

        _repository = new EFCoreAIAgentRepository(new TestEFCoreScopeProvider(() => CreateContext(withRecorder: true)));
    }

    public void Dispose() => _connection.Dispose();

    #region Happy path

    [Fact]
    public async Task GetAll_ForMember_ReturnsLimitedAgent()
    {
        var limited = await SeedAsync("limited", [Editors]);

        var listed = await _repository.GetAllAsync([Editors]);

        listed.Select(a => a.Id).ShouldBe([limited]);
    }

    [Fact]
    public async Task GetAll_ForMemberOfOneOfSeveralGroups_ReturnsLimitedAgent()
    {
        var limited = await SeedAsync("limited", [Editors, Translators]);

        var listed = await _repository.GetAllAsync([Translators]);

        listed.Select(a => a.Id).ShouldBe([limited]);
    }

    [Fact]
    public async Task GetAll_ForUserInNoGroups_ReturnsOnlyAgentsWithNoLimit()
    {
        await SeedAsync("limited", [Editors]);
        var open = await SeedAsync("open", []);

        var listed = await _repository.GetAllAsync([]);

        listed.Select(a => a.Id).ShouldBe([open]);
    }

    [Fact]
    public async Task GetAll_ForAgentWithAnEmptyStringColumn_TreatsItAsNoLimit()
    {
        var id = await SeedAsync("open", []);
        await using (var db = CreateContext(withRecorder: false))
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE umbracoAIAgent SET AllowedUserGroupIds = '' WHERE Id = {0}", id);
        }

        var listed = await _repository.GetAllAsync([Translators]);

        listed.Select(a => a.Id).ShouldBe([id]);
    }

    [Fact]
    public async Task GetAll_ForAgentWithAnEmptyArrayColumn_TreatsItAsNoLimit()
    {
        var id = await SeedAsync("open", []);
        await using (var db = CreateContext(withRecorder: false))
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE umbracoAIAgent SET AllowedUserGroupIds = '[]' WHERE Id = {0}", id);
        }

        var listed = await _repository.GetAllAsync([Translators]);

        listed.Select(a => a.Id).ShouldBe([id]);
    }

    [Fact]
    public async Task GetAll_WithNoGroupFilter_ReturnsEveryAgent()
    {
        await SeedAsync("limited", [Editors]);
        await SeedAsync("open", []);

        var listed = await _repository.GetAllAsync();

        listed.Count().ShouldBe(2);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task GetAll_ForNonMember_ExcludesLimitedAgent()
    {
        await SeedAsync("limited", [Editors]);

        var listed = await _repository.GetAllAsync([Translators]);

        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListFilter_OnlyDeletedGroup_MatchesNoOne()
    {
        // Given an agent limited only to a deleted group's key
        await SeedAsync("orphaned", [DeletedGroup]);

        // When a non-manager lists agents
        var listed = await _repository.GetAllAsync([Editors, Translators]);
        var (paged, total) = await _repository.GetPagedAsync(0, 10, usableByGroupKeys: [Editors, Translators]);
        var bySurface = await _repository.GetBySurfaceAsync("copilot", [Editors, Translators]);

        // Then the agent isn't listed (GP-7 AC7)
        listed.ShouldBeEmpty();
        paged.ShouldBeEmpty();
        total.ShouldBe(0);
        bySurface.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPaged_ForNonMember_ExcludesLimitedAgentFromItemsAndTotal()
    {
        await SeedAsync("limited", [Editors]);
        var open = await SeedAsync("open", []);

        var (items, total) = await _repository.GetPagedAsync(0, 10, usableByGroupKeys: [Translators]);

        items.Select(a => a.Id).ShouldBe([open]);
        total.ShouldBe(1);
    }

    [Fact]
    public async Task GetPaged_KeepsTheOtherFiltersAlongsideTheGroupFilter()
    {
        await SeedAsync("limited-inactive", [Editors], isActive: false);
        var active = await SeedAsync("limited-active", [Editors]);

        var (items, total) = await _repository.GetPagedAsync(0, 10, isActive: true, usableByGroupKeys: [Editors]);

        items.Select(a => a.Id).ShouldBe([active]);
        total.ShouldBe(1);
    }

    [Fact]
    public async Task GetBySurface_ForNonMember_ExcludesLimitedAgent()
    {
        await SeedAsync("limited", [Editors], surfaces: ["copilot"]);
        var open = await SeedAsync("open", [], surfaces: ["copilot"]);

        var listed = await _repository.GetBySurfaceAsync("copilot", [Translators]);

        listed.Select(a => a.Id).ShouldBe([open]);
    }

    #endregion

    #region Query shape

    [Fact]
    public async Task GetAll_FiltersInOneQuery_WhateverTheNumberOfAgents()
    {
        // 30 agents, a mix of limited and open: the cost of the filter must not grow with the row count.
        for (var i = 0; i < 30; i++)
        {
            await SeedAsync($"agent-{i}", i % 2 == 0 ? [Editors] : []);
        }

        _commands.Clear();
        var listed = await _repository.GetAllAsync([Editors, Translators]);

        listed.Count().ShouldBe(30);
        _commands.Texts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAll_OrsOneLikePerGroupWithTheLowercaseQuotedKey()
    {
        await SeedAsync("limited", [Editors]);

        _commands.Clear();
        await _repository.GetAllAsync([Editors, Translators]);

        var sql = _commands.Texts.Single();
        CountOccurrences(sql, " LIKE ").ShouldBe(2);
        _commands.ParameterValues.ShouldContain($"%\"{Editors:D}\"%");
        _commands.ParameterValues.ShouldContain($"%\"{Translators:D}\"%");
    }

    [Fact]
    public async Task GetAll_SendsTheGroupPatternsAsParametersNotInlinedIntoTheSql()
    {
        await SeedAsync("limited", [Editors]);

        _commands.Clear();
        await _repository.GetAllAsync([Editors]);

        _commands.Texts.Single().ShouldNotContain(Editors.ToString("D"));
    }

    [Fact]
    public async Task GetPaged_FiltersTheCountAndThePageInTheSameWay_TwoQueriesInTotal()
    {
        await SeedAsync("limited", [Editors]);

        _commands.Clear();
        await _repository.GetPagedAsync(0, 10, usableByGroupKeys: [Translators]);

        _commands.Texts.Count.ShouldBe(2);
        _commands.ParameterValues.ShouldContain($"%\"{Translators:D}\"%");
    }

    #endregion

    private async Task<Guid> SeedAsync(
        string alias,
        IReadOnlyList<Guid> allowedGroups,
        bool isActive = true,
        IReadOnlyList<string>? surfaces = null)
    {
        var agent = new AIAgent
        {
            Id = Guid.NewGuid(),
            Alias = alias,
            Name = alias,
            ProfileId = Guid.NewGuid(),
            Config = new AIStandardAgentConfig(),
            IsActive = isActive,
            AllowedUserGroupIds = allowedGroups,
            SurfaceIds = surfaces ?? [],
        };

        await using var db = CreateContext(withRecorder: false);
        db.Agents.Add(AIAgentEntityFactory.BuildEntity(agent));
        await db.SaveChangesAsync();
        return agent.Id;
    }

    private UmbracoAIAgentDbContext CreateContext(bool withRecorder)
    {
        var builder = new DbContextOptionsBuilder<UmbracoAIAgentDbContext>()
            .UseSqlite(_connection, x =>
            {
                x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.Sqlite");
                x.MigrationsHistoryTable(AIConnectionStringResolver.MigrationsHistoryTableName);
            });

        if (withRecorder)
        {
            builder.AddInterceptors(_commands);
        }

        return new UmbracoAIAgentDbContext(builder.Options);
    }

    private static int CountOccurrences(string text, string value)
        => (text.Length - text.Replace(value, string.Empty).Length) / value.Length;

    /// <summary>Records the text of every SQL command the repository runs.</summary>
    private sealed class CommandRecorder : DbCommandInterceptor
    {
        private readonly List<string> _texts = [];
        private readonly List<object?> _parameterValues = [];

        public IReadOnlyList<string> Texts => _texts;

        public IReadOnlyList<object?> ParameterValues => _parameterValues;

        public void Clear()
        {
            _texts.Clear();
            _parameterValues.Clear();
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _texts.Add(command.CommandText);
            _parameterValues.AddRange(command.Parameters.Cast<DbParameter>().Select(p => p.Value));
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

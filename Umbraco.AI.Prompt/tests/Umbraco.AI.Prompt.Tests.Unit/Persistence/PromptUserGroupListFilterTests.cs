// GP-13 (AC2): the list filter runs in the database query

using System.Data.Common;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Shouldly;

using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Persistence;
using Umbraco.AI.Prompt.Persistence.Prompts;
using Umbraco.AI.Prompt.Tests.Unit.Fixtures;

using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Persistence;

/// <summary>
/// The prompt list queries keep only the prompts a set of user groups may use, in the same SQL query, against
/// the real SQLite migrations.
/// </summary>
public class PromptUserGroupListFilterTests : IDisposable
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();
    private static readonly Guid DeletedGroup = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly CommandRecorder _commands = new();
    private readonly EFCoreAIPromptRepository _repository;

    public PromptUserGroupListFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (var db = CreateContext(withRecorder: false))
        {
            db.Database.Migrate();
        }

        _repository = new EFCoreAIPromptRepository(new TestEFCoreScopeProvider(() => CreateContext(withRecorder: true)));
    }

    public void Dispose() => _connection.Dispose();

    #region Happy path

    [Fact]
    public async Task GetAll_ForMember_ReturnsLimitedPrompt()
    {
        var limited = await SeedAsync("limited", [Editors]);

        var listed = await _repository.GetAllAsync([Editors]);

        listed.Select(p => p.Id).ShouldBe([limited]);
    }

    [Fact]
    public async Task GetAll_ForMemberOfOneOfSeveralGroups_ReturnsLimitedPrompt()
    {
        var limited = await SeedAsync("limited", [Editors, Translators]);

        var listed = await _repository.GetAllAsync([Translators]);

        listed.Select(p => p.Id).ShouldBe([limited]);
    }

    [Fact]
    public async Task GetAll_ForUserInNoGroups_ReturnsOnlyPromptsWithNoLimit()
    {
        await SeedAsync("limited", [Editors]);
        var open = await SeedAsync("open", []);

        var listed = await _repository.GetAllAsync([]);

        listed.Select(p => p.Id).ShouldBe([open]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    public async Task GetAll_ForPromptWithAnEmptyColumn_TreatsItAsNoLimit(string column)
    {
        var id = await SeedAsync("open", []);
        await using (var db = CreateContext(withRecorder: false))
        {
            await db.Database.ExecuteSqlRawAsync("UPDATE umbracoAIPrompt SET AllowedUserGroupIds = {0} WHERE Id = {1}", column, id);
        }

        var listed = await _repository.GetAllAsync([Translators]);

        listed.Select(p => p.Id).ShouldBe([id]);
    }

    [Fact]
    public async Task GetAll_WithNoGroupFilter_ReturnsEveryPrompt()
    {
        await SeedAsync("limited", [Editors]);
        await SeedAsync("open", []);

        var listed = await _repository.GetAllAsync();

        listed.Count().ShouldBe(2);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task GetAll_ForNonMember_ExcludesLimitedPrompt()
    {
        await SeedAsync("limited", [Editors]);

        var listed = await _repository.GetAllAsync([Translators]);

        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListFilter_OnlyDeletedGroup_MatchesNoOne()
    {
        // Given a prompt limited only to a deleted group's key
        await SeedAsync("orphaned", [DeletedGroup]);

        // When a non-manager lists prompts
        var listed = await _repository.GetAllAsync([Editors, Translators]);
        var (paged, total) = await _repository.GetPagedAsync(0, 10, usableByGroupKeys: [Editors, Translators]);

        // Then the prompt isn't listed
        listed.ShouldBeEmpty();
        paged.ShouldBeEmpty();
        total.ShouldBe(0);
    }

    [Fact]
    public async Task GetPaged_ForNonMember_ExcludesLimitedPromptFromItemsAndTotal()
    {
        await SeedAsync("limited", [Editors]);
        var open = await SeedAsync("open", []);

        var (items, total) = await _repository.GetPagedAsync(0, 10, usableByGroupKeys: [Translators]);

        items.Select(p => p.Id).ShouldBe([open]);
        total.ShouldBe(1);
    }

    [Fact]
    public async Task GetPaged_KeepsTheOtherFiltersAlongsideTheGroupFilter()
    {
        var profileA = Guid.NewGuid();
        await SeedAsync("limited-other-profile", [Editors], profileId: Guid.NewGuid());
        var matching = await SeedAsync("limited-matching", [Editors], profileId: profileA);

        var (items, total) = await _repository.GetPagedAsync(0, 10, profileId: profileA, usableByGroupKeys: [Editors]);

        items.Select(p => p.Id).ShouldBe([matching]);
        total.ShouldBe(1);
    }

    #endregion

    #region Query shape

    [Fact]
    public async Task GetAll_FiltersInOneQuery_WhateverTheNumberOfPrompts()
    {
        // 30 prompts, a mix of limited and open: the cost of the filter must not grow with the row count.
        for (var i = 0; i < 30; i++)
        {
            await SeedAsync($"prompt-{i}", i % 2 == 0 ? [Editors] : []);
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

    private async Task<Guid> SeedAsync(string alias, IReadOnlyList<Guid> allowedGroups, Guid? profileId = null)
    {
        var prompt = new AIPrompt
        {
            Id = Guid.NewGuid(),
            Alias = alias,
            Name = alias,
            Instructions = "Do it",
            ProfileId = profileId,
            AllowedUserGroupIds = allowedGroups,
        };

        await using var db = CreateContext(withRecorder: false);
        db.Prompts.Add(AIPromptEntityFactory.BuildEntity(prompt));
        await db.SaveChangesAsync();
        return prompt.Id;
    }

    private UmbracoAIPromptDbContext CreateContext(bool withRecorder)
    {
        var builder = new DbContextOptionsBuilder<UmbracoAIPromptDbContext>()
            .UseSqlite(_connection, x =>
            {
                x.MigrationsAssembly("Umbraco.AI.Prompt.Persistence.Sqlite");
                x.MigrationsHistoryTable(AIConnectionStringResolver.MigrationsHistoryTableName);
            });

        if (withRecorder)
        {
            builder.AddInterceptors(_commands);
        }

        return new UmbracoAIPromptDbContext(builder.Options);
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

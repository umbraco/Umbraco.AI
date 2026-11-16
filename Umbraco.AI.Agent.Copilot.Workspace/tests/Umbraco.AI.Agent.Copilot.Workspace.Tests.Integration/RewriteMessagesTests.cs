// S3 - Rewrite stored messages in one step (AC1-AC3)
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moq;

using NUnit.Framework;

using Shouldly;

using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Persistence;
using Umbraco.AI.Agent.Conversations.Persistence.Conversations;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration;

/// <summary>
/// Feature: the conversation store rewrites messages (updates and deletes) in one transaction.
/// Runs the real repository on SQLite, against the real conversation migrations.
/// </summary>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewEmptyPerTest)]
internal sealed class RewriteMessagesTests : UmbracoIntegrationTest
{
    private EFCoreAIConversationRepository _repository = null!;
    private Guid _conversationId;

    // GlobalSetupTeardown is not discovered from the referenced package, so supply the test-database
    // settings directly (same as AmbientScopeRaceTests).
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);

        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "4",
            ["Tests:Database:SchemaDatabaseCount"] = "4",
            ["Tests:Database:EmptyDatabasesCount"] = "2",
        });
    }

    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.Services.AddUmbracoDbContext<UmbracoAIConversationsDbContext>(
            (options, connectionString, providerName, _) =>
                UmbracoAIConversationsDbContext.ConfigureProvider(options, connectionString, providerName),
            shareUmbracoConnection: true);
    }

    [SetUp]
    public async Task CreateRepositoryAndConversation()
    {
        // Apply the real conversation migrations to the test database.
        await using (var migrationContext = new UmbracoAIConversationsDbContext(BuildOptions()))
        {
            await migrationContext.Database.MigrateAsync();
        }

        // The factory is only used by conversation reads and writes; a rewrite never touches it.
        _repository = new EFCoreAIConversationRepository(
            GetRequiredService<IEFCoreScopeProvider<UmbracoAIConversationsDbContext>>(),
            new AIConversationEntityFactory(
                Mock.Of<IAIEditableModelSerializer>(),
                new AIContextResourceTypeCollection(() => [])));

        _conversationId = await CreateConversationWithMessagesAsync(4);
    }

    // Scenario: a rewrite succeeds (happy path)

    // AC1 - Given a conversation with stored messages; when a rewrite updates one message's content,
    // then that message holds the new content and keeps its Sequence and DateCreated
    [Test]
    public async Task Rewrite_WithAnUpdate_SavesTheNewContentAndKeepsSequenceAndDateCreated()
    {
        IReadOnlyList<AIMessage> before = await _repository.GetMessagesAsync(_conversationId);
        AIMessage target = before[1];

        await _repository.RewriteMessagesAsync(
            _conversationId,
            [new AIMessage { Id = target.Id, ContentJson = "{\"new\":true}", ContentText = "new text" }],
            []);

        AIMessage after = (await _repository.GetMessagesAsync(_conversationId)).Single(m => m.Id == target.Id);
        after.ContentJson.ShouldBe("{\"new\":true}");
        after.ContentText.ShouldBe("new text");
        after.Sequence.ShouldBe(target.Sequence);
        after.DateCreated.ShouldBe(target.DateCreated);
        after.InputTokens.ShouldBe(target.InputTokens);
        after.OutputTokens.ShouldBe(target.OutputTokens);
        after.Role.ShouldBe(target.Role);
    }

    // AC2 - Given a conversation with stored messages; when a rewrite deletes two messages by id,
    // then those messages are gone and the rest keep their order
    [Test]
    public async Task Rewrite_WithTwoDeletes_RemovesThoseMessagesAndKeepsTheRestInOrder()
    {
        IReadOnlyList<AIMessage> before = await _repository.GetMessagesAsync(_conversationId);

        await _repository.RewriteMessagesAsync(_conversationId, [], [before[1].Id, before[2].Id]);

        IReadOnlyList<AIMessage> after = await _repository.GetMessagesAsync(_conversationId);
        after.Select(m => m.Id).ShouldBe([before[0].Id, before[3].Id]);
        after.Select(m => m.Sequence).ShouldBe([before[0].Sequence, before[3].Sequence]);
    }

    // Scenario: sad path

    // AC3 - Given a rewrite with one update and one delete; when one part fails, then nothing is saved.
    // EF Core sends deletes before updates, so the delete succeeds first and the failure is forced on the
    // update that runs last; atomicity means the already-sent delete must be rolled back too.
    [Test]
    public async Task Rewrite_WhenTheLaterUpdateFails_RollsBackTheEarlierDelete()
    {
        IReadOnlyList<AIMessage> before = await _repository.GetMessagesAsync(_conversationId);

        // Make every message update fail inside the database itself.
        await using (var db = new UmbracoAIConversationsDbContext(BuildOptions()))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER failUpdate BEFORE UPDATE ON umbracoAIConversationsMessage " +
                "BEGIN SELECT RAISE(ABORT, 'update blocked'); END;");
        }

        await Should.ThrowAsync<Exception>(() => _repository.RewriteMessagesAsync(
            _conversationId,
            [new AIMessage { Id = before[0].Id, ContentJson = "{\"new\":true}", ContentText = "new text" }],
            [before[3].Id]));

        IReadOnlyList<AIMessage> after = await _repository.GetMessagesAsync(_conversationId);
        after.Select(m => m.Id).ShouldContain(before[3].Id);
        after.Count.ShouldBe(before.Count);
        after[0].ContentJson.ShouldBe(before[0].ContentJson);
        after[0].ContentText.ShouldBe(before[0].ContentText);
    }

    // Edge (not in the stories): the same id twice in the updates is rejected with a clear message.
    [Test]
    public async Task Rewrite_WithADuplicateUpdateId_ThrowsAnArgumentExceptionNamingTheId()
    {
        IReadOnlyList<AIMessage> before = await _repository.GetMessagesAsync(_conversationId);
        Guid id = before[0].Id;

        ArgumentException ex = await Should.ThrowAsync<ArgumentException>(() => _repository.RewriteMessagesAsync(
            _conversationId,
            [
                new AIMessage { Id = id, ContentJson = "{}", ContentText = "a" },
                new AIMessage { Id = id, ContentJson = "{}", ContentText = "b" },
            ],
            []));

        ex.Message.ShouldContain(id.ToString());
        ex.Message.ShouldContain("more than once");
    }

    // Edge (not in the stories): a rewrite is scoped to its conversation, so ids from another conversation
    // are ignored.
    [Test]
    public async Task Rewrite_WithIdsFromAnotherConversation_LeavesThoseMessagesUntouched()
    {
        Guid otherId = await CreateConversationWithMessagesAsync(2);
        IReadOnlyList<AIMessage> other = await _repository.GetMessagesAsync(otherId);

        await _repository.RewriteMessagesAsync(
            _conversationId,
            [new AIMessage { Id = other[0].Id, ContentJson = "{\"new\":true}", ContentText = "new text" }],
            [other[1].Id]);

        IReadOnlyList<AIMessage> after = await _repository.GetMessagesAsync(otherId);
        after.Count.ShouldBe(2);
        after[0].ContentJson.ShouldBe(other[0].ContentJson);
    }

    private DbContextOptions<UmbracoAIConversationsDbContext> BuildOptions()
    {
        ConnectionStrings connection = GetRequiredService<IOptionsMonitor<ConnectionStrings>>().CurrentValue;
        var optionsBuilder = new DbContextOptionsBuilder<UmbracoAIConversationsDbContext>();
        UmbracoAIConversationsDbContext.ConfigureProvider(optionsBuilder, connection.ConnectionString, connection.ProviderName);
        return optionsBuilder.Options;
    }

    private async Task<Guid> CreateConversationWithMessagesAsync(int messageCount)
    {
        var id = Guid.NewGuid();
        await _repository.CreateAsync(new AIConversation
        {
            Id = id,
            UserKey = Guid.NewGuid(),
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow,
        });

        await _repository.AddMessagesAsync(
            id,
            Enumerable.Range(0, messageCount)
                .Select(i => new AIMessage
                {
                    Id = Guid.NewGuid(),
                    Role = i % 2 == 0 ? "user" : "assistant",
                    ContentJson = $"{{\"i\":{i}}}",
                    ContentText = $"text {i}",
                    InputTokens = i,
                    OutputTokens = i * 2,
                })
                .ToList());

        return id;
    }
}

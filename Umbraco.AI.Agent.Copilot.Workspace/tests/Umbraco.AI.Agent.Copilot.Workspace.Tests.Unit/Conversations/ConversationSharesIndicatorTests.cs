using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Persistence;
using Umbraco.AI.Agent.Conversations.Persistence.Conversations;
using Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Fixtures;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.EditableModels;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US2 AC5-6 (backend half) — the owner's tree-item entity-sign badge (SC-13) is driven
/// by a <c>HasActiveShares</c> field computed in the existing paged-list query, not a follow-up query
/// per conversation (ARCHITECTURE.md decision 6). This is a genuine query-shape behavior (a
/// correlated subquery inside <see cref="EFCoreAIConversationRepository.GetPagedAsync"/>), so it runs
/// against a real in-memory SQLite database rather than a mock — a mocked repository can't meaningfully
/// prove the projection is shaped correctly. This file covers the computation only; the badge's
/// rendering is specs/frontend/shared-indicator.spec.ts.
/// </summary>
public sealed class ConversationSharesIndicatorTests : IDisposable
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    private readonly SqliteConnection _connection;
    private readonly AIConversationEntityFactory _factory;

    public ConversationSharesIndicatorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using (UmbracoAIConversationsDbContext db = CreateContext())
        {
            db.Database.EnsureCreated();
        }

        _factory = new AIConversationEntityFactory(
            Mock.Of<IAIEditableModelSerializer>(),
            new AIContextResourceTypeCollection(() => []));
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    #region Happy path

    [Fact]
    public async Task GetConversationsPagedAsync_ConversationWithActiveShare_HasActiveSharesTrue()
    {
        // AC5 (backend half) — a conversation with at least one non-revoked share row reports
        // HasActiveShares: true in the paged list, without a per-row follow-up query.
        var conversationId = Guid.NewGuid();
        using (UmbracoAIConversationsDbContext db = CreateContext())
        {
            db.Conversations.Add(BuildConversation(conversationId));
            db.ConversationShares.Add(BuildShare(conversationId, dateRevoked: null));
            await db.SaveChangesAsync();
        }

        (IReadOnlyList<AIConversation> items, _) =
            await BuildRepository().GetPagedAsync(OwnerKey, 0, 10);

        items.Single(c => c.Id == conversationId).HasActiveShares.ShouldBeTrue();
    }

    [Fact]
    public async Task GetConversationsPagedAsync_ConversationWithOnlyRevokedShares_HasActiveSharesFalse()
    {
        // A conversation whose only shares are all revoked reports HasActiveShares: false — the
        // field reflects current exposure, not sharing history.
        var conversationId = Guid.NewGuid();
        using (UmbracoAIConversationsDbContext db = CreateContext())
        {
            db.Conversations.Add(BuildConversation(conversationId));
            db.ConversationShares.Add(BuildShare(conversationId, dateRevoked: DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        (IReadOnlyList<AIConversation> items, _) =
            await BuildRepository().GetPagedAsync(OwnerKey, 0, 10);

        items.Single(c => c.Id == conversationId).HasActiveShares.ShouldBeFalse();
    }

    [Fact]
    public async Task GetConversationsPagedAsync_TwoConversationsSameOwner_FlagIsPerConversationNotGlobal()
    {
        // Guards the correlation itself: without `s.ConversationId == c.Id` in the subquery, ANY
        // active share anywhere would flip every conversation's flag to true. With two conversations
        // for the same owner in the same page — one actively shared, one with only a revoked share —
        // the flag must differ between them.
        var sharedConversationId = Guid.NewGuid();
        var unsharedConversationId = Guid.NewGuid();
        using (UmbracoAIConversationsDbContext db = CreateContext())
        {
            db.Conversations.Add(BuildConversation(sharedConversationId));
            db.Conversations.Add(BuildConversation(unsharedConversationId));
            db.ConversationShares.Add(BuildShare(sharedConversationId, dateRevoked: null));
            db.ConversationShares.Add(BuildShare(unsharedConversationId, dateRevoked: DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        (IReadOnlyList<AIConversation> items, _) =
            await BuildRepository().GetPagedAsync(OwnerKey, 0, 10);

        items.Single(c => c.Id == sharedConversationId).HasActiveShares.ShouldBeTrue();
        items.Single(c => c.Id == unsharedConversationId).HasActiveShares.ShouldBeFalse();
    }

    [Fact]
    public async Task GetConversationsPagedAsync_PinnedAndShared_BothFlagsTrueIndependently()
    {
        // AC6 (backend half) — IsPinned and HasActiveShares are independent fields; nothing in the
        // query makes one exclude the other. (The frontend renders both simultaneously — this spec
        // only guards that the data supports it.)
        var conversationId = Guid.NewGuid();
        using (UmbracoAIConversationsDbContext db = CreateContext())
        {
            AIConversationEntity conversation = BuildConversation(conversationId);
            conversation.IsPinned = true;
            db.Conversations.Add(conversation);
            db.ConversationShares.Add(BuildShare(conversationId, dateRevoked: null));
            await db.SaveChangesAsync();
        }

        (IReadOnlyList<AIConversation> items, _) =
            await BuildRepository().GetPagedAsync(OwnerKey, 0, 10);

        AIConversation result = items.Single(c => c.Id == conversationId);
        result.IsPinned.ShouldBeTrue();
        result.HasActiveShares.ShouldBeTrue();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task GetConversationsPagedAsync_NoShares_HasActiveSharesFalse()
    {
        // Regression guard: a conversation that's never been shared reports HasActiveShares: false,
        // not null/undefined — existing callers of this response model shouldn't need a null check.
        var conversationId = Guid.NewGuid();
        using (UmbracoAIConversationsDbContext db = CreateContext())
        {
            db.Conversations.Add(BuildConversation(conversationId));
            await db.SaveChangesAsync();
        }

        (IReadOnlyList<AIConversation> items, _) =
            await BuildRepository().GetPagedAsync(OwnerKey, 0, 10);

        items.Single(c => c.Id == conversationId).HasActiveShares.ShouldBeFalse();
    }

    #endregion

    private static AIConversationEntity BuildConversation(Guid id)
    {
        var now = DateTime.UtcNow;
        return new AIConversationEntity
        {
            Id = id,
            UserKey = OwnerKey,
            Title = "Test conversation",
            DateCreated = now,
            DateModified = now,
            LastMessageAt = now,
            Version = 1,
        };
    }

    private static AIConversationShareEntity BuildShare(Guid conversationId, DateTime? dateRevoked) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        SharedWithUserKey = RecipientKey,
        SharedByUserKey = OwnerKey,
        ScopeMode = 1, // Live
        CutoffMessageId = null,
        DateShared = DateTime.UtcNow,
        DateRevoked = dateRevoked,
    };

    private UmbracoAIConversationsDbContext CreateContext()
    {
        DbContextOptions<UmbracoAIConversationsDbContext> options = new DbContextOptionsBuilder<UmbracoAIConversationsDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new UmbracoAIConversationsDbContext(options);
    }

    private EFCoreAIConversationRepository BuildRepository() =>
        new(new TestEFCoreScopeProvider(CreateContext), _factory);
}

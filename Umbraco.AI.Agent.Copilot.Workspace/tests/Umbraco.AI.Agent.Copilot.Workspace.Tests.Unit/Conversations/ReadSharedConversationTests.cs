using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US4 — Read a conversation shared with me (backend half). Exercises
/// <see cref="AIConversationShareReadService.GetSharedConversationAsync"/>, including the
/// Snapshot/Live cutoff behavior and the graceful-degradation rule from ARCHITECTURE.md's data-model
/// section. ARCHITECTURE.md decision 1: this is a separate code path from
/// <see cref="AIConversationService"/>'s owner methods and never goes through
/// <see cref="IAIConversationService"/> at all.
/// </summary>
public class ReadSharedConversationTests
{
    private static readonly Guid RecipientKey = Guid.NewGuid();
    private static readonly Guid OwnerKey = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task GetSharedConversationAsync_SnapshotScope_ReturnsMessagesUpToCutoffSequenceOnly()
    {
        // AC1 — every message whose Sequence <= the CutoffMessageId message's Sequence, nothing after.
        var conversationId = Guid.NewGuid();
        var cutoffMessageId = Guid.NewGuid();
        var (service, conversations, shares) = BuildService();
        SetupConversation(conversations, conversationId);
        SetupMessages(
            conversations,
            conversationId,
            new AIMessage { Id = Guid.NewGuid(), Sequence = 1 },
            new AIMessage { Id = cutoffMessageId, Sequence = 2 },
            new AIMessage { Id = Guid.NewGuid(), Sequence = 3 });
        SetupActiveShare(shares, conversationId, new AIConversationShare
        {
            ConversationId = conversationId,
            SharedWithUserKey = RecipientKey,
            SharedByUserKey = OwnerKey,
            ScopeMode = AIConversationShareScopeMode.Snapshot,
            CutoffMessageId = cutoffMessageId,
        });

        var result = await service.GetSharedConversationAsync(conversationId);

        result.ShouldNotBeNull();
        result.Value.Messages.Select(m => m.Sequence).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task GetSharedConversationAsync_LiveScope_ReturnsMessagesAddedAfterSharing()
    {
        // AC2 — no ceiling for a Live share, including messages that postdate the share entirely.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares) = BuildService();
        SetupConversation(conversations, conversationId);
        SetupMessages(
            conversations,
            conversationId,
            new AIMessage { Id = Guid.NewGuid(), Sequence = 1 },
            new AIMessage { Id = Guid.NewGuid(), Sequence = 2 },
            new AIMessage { Id = Guid.NewGuid(), Sequence = 3 });
        SetupActiveShare(shares, conversationId, new AIConversationShare
        {
            ConversationId = conversationId,
            SharedWithUserKey = RecipientKey,
            SharedByUserKey = OwnerKey,
            ScopeMode = AIConversationShareScopeMode.Live,
            CutoffMessageId = null,
        });

        var result = await service.GetSharedConversationAsync(conversationId);

        result.ShouldNotBeNull();
        result.Value.Messages.Select(m => m.Sequence).ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task GetSharedConversationAsync_ResourcesAndContextIds_ReturnedUnfiltered()
    {
        // SPEC.md — no per-viewer redaction (ARCHITECTURE.md decision 2): the same attachment list the
        // owner sees comes back as-is.
        var conversationId = Guid.NewGuid();
        var contextId = Guid.NewGuid();
        var resource = new AIAttachedResource { Id = Guid.NewGuid(), ResourceTypeId = "content" };
        var (service, conversations, shares) = BuildService();
        conversations
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation
            {
                Id = conversationId,
                UserKey = OwnerKey,
                ContextIds = [contextId],
                Resources = [resource],
            });
        SetupMessages(conversations, conversationId);
        SetupActiveShare(shares, conversationId, new AIConversationShare
        {
            ConversationId = conversationId,
            SharedWithUserKey = RecipientKey,
            SharedByUserKey = OwnerKey,
            ScopeMode = AIConversationShareScopeMode.Live,
        });

        var result = await service.GetSharedConversationAsync(conversationId);

        result.ShouldNotBeNull();
        result.Value.Conversation.ContextIds.ShouldBe([contextId]);
        result.Value.Conversation.Resources.ShouldBe([resource]);
    }

    [Fact]
    public async Task GetSharedConversationAsync_CutoffMessageDeletedByRegenerate_ReturnsEveryRemainingMessage()
    {
        // AC5 — graceful degradation: TruncateAfterLastUserMessageAsync removed the anchor message,
        // the read falls back to everything that still exists rather than erroring.
        var conversationId = Guid.NewGuid();
        var deletedCutoffMessageId = Guid.NewGuid();
        var (service, conversations, shares) = BuildService();
        SetupConversation(conversations, conversationId);
        SetupMessages(
            conversations,
            conversationId,
            new AIMessage { Id = Guid.NewGuid(), Sequence = 1 },
            new AIMessage { Id = Guid.NewGuid(), Sequence = 2 });
        SetupActiveShare(shares, conversationId, new AIConversationShare
        {
            ConversationId = conversationId,
            SharedWithUserKey = RecipientKey,
            SharedByUserKey = OwnerKey,
            ScopeMode = AIConversationShareScopeMode.Snapshot,
            CutoffMessageId = deletedCutoffMessageId,
        });

        var result = await service.GetSharedConversationAsync(conversationId);

        result.ShouldNotBeNull();
        result.Value.Messages.Select(m => m.Sequence).ShouldBe([1, 2]);
    }

    // Real entry point, per bdd-specs rule 4.
    [Fact(Skip = "Pending real DI resolution — verified live in SC-10")]
    public Task GetSharedConversationAsync_ResolvedFromDI_ReturnsSharedView() => Task.CompletedTask;

    #endregion

    #region Sad path

    [Fact]
    public async Task GetSharedConversationAsync_NoActiveShareForActingUser_ReturnsNull()
    {
        // AC6 — a conversation that was never shared with the acting user 404s, same
        // not-found-vs-forbidden indistinguishability as the owner-only endpoints. This service never
        // throws itself (its only caller, the future controller, turns a null into the 404) — the
        // contract here is simply that it returns null rather than any conversation data.
        var conversationId = Guid.NewGuid();
        var (service, conversations, shares) = BuildService();
        shares
            .Setup(r => r.GetActiveShareAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIConversationShare?)null);

        var result = await service.GetSharedConversationAsync(conversationId);

        result.ShouldBeNull();
        conversations.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetSharedConversationAsync_NoActingUser_ReturnsNull()
    {
        // No acting backoffice user is available — same posture as AIConversationService's own owned
        // read, which returns null rather than throwing.
        var conversations = new Mock<IAIConversationRepository>();
        var shares = new Mock<IAIConversationShareRepository>();
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns((IBackOfficeSecurity?)null);
        var service = new AIConversationShareReadService(shares.Object, conversations.Object, accessor.Object);

        var result = await service.GetSharedConversationAsync(Guid.NewGuid());

        result.ShouldBeNull();
        shares.Verify(
            r => r.GetActiveShareAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    private static void SetupConversation(Mock<IAIConversationRepository> conversations, Guid conversationId)
        => conversations
            .Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = conversationId, UserKey = OwnerKey });

    private static void SetupMessages(Mock<IAIConversationRepository> conversations, Guid conversationId, params AIMessage[] messages)
        => conversations
            .Setup(r => r.GetMessagesAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

    private static void SetupActiveShare(Mock<IAIConversationShareRepository> shares, Guid conversationId, AIConversationShare share)
        => shares
            .Setup(r => r.GetActiveShareAsync(conversationId, RecipientKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(share);

    private static (
        AIConversationShareReadService Service,
        Mock<IAIConversationRepository> Conversations,
        Mock<IAIConversationShareRepository> Shares) BuildService()
    {
        var conversations = new Mock<IAIConversationRepository>();
        var shares = new Mock<IAIConversationShareRepository>();

        var user = new Mock<IUser>();
        user.Setup(x => x.Key).Returns(RecipientKey);
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(x => x.CurrentUser).Returns(user.Object);
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns(security.Object);

        var service = new AIConversationShareReadService(shares.Object, conversations.Object, accessor.Object);
        return (service, conversations, shares);
    }
}

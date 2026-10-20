using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US3 — Find conversations shared with me (backend half). Exercises
/// <see cref="AIConversationShareReadService.GetSharedConversationsPagedAsync"/>. The frontend half
/// (sidebar rendering) is specs/frontend/find-shared-conversations.spec.ts.
/// </summary>
public class FindSharedConversationsTests
{
    private static readonly Guid RecipientKey = Guid.NewGuid();
    private static readonly Guid OwnerKey = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task GetSharedConversationsPagedAsync_ActiveShareForActingUser_IncludesItWithOwnerAndScope()
    {
        // AC1 (backend half) — an active share for the acting-as-recipient user appears in the page,
        // including the owner's key (used to resolve the frontend's hover-tooltip text, AC2) and the
        // scope mode it was shared under.
        var conversationId = Guid.NewGuid();
        var (service, shares) = BuildService();
        var expected = new[]
        {
            new AISharedConversationSummary
            {
                ConversationId = conversationId,
                Title = "Debugging the checkout redirect",
                OwnerUserKey = OwnerKey,
                ScopeMode = AIConversationShareScopeMode.Live,
                DateShared = DateTime.UtcNow,
            },
        };
        shares
            .Setup(r => r.GetSharedWithUserPagedAsync(RecipientKey, 0, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((expected, 1));

        var (items, total) = await service.GetSharedConversationsPagedAsync(0, 10);

        total.ShouldBe(1);
        items.ShouldBe(expected);
    }

    // Real entry point, per bdd-specs rule 4.
    [Fact(Skip = "Pending the Management API controller — SC-09")]
    public Task GetSharedConversationsPagedAsync_ResolvedFromDI_ReturnsSharedConversation() => Task.CompletedTask;

    #endregion

    #region Sad path

    [Fact]
    public async Task GetSharedConversationsPagedAsync_AfterRevoke_NoLongerIncludesIt()
    {
        // AC3 (backend half) — a revoked share drops out of the paged result. The service never
        // special-cases revocation itself; it is a thin pass-through, so this guards that the
        // repository's own "active shares only" contract (see IAIConversationShareRepository) is
        // actually what the caller receives.
        var (service, shares) = BuildService();
        shares
            .Setup(r => r.GetSharedWithUserPagedAsync(RecipientKey, 0, 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AISharedConversationSummary>(), 0));

        var (items, total) = await service.GetSharedConversationsPagedAsync(0, 10);

        total.ShouldBe(0);
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetSharedConversationsPagedAsync_NoActingUser_ReturnsEmptyPage()
    {
        // No acting backoffice user is available — same posture as AIConversationService's own paged
        // list, which returns an empty page rather than throwing.
        var shares = new Mock<IAIConversationShareRepository>();
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.Setup(x => x.BackOfficeSecurity).Returns((IBackOfficeSecurity?)null);
        var service = new AIConversationShareReadService(shares.Object, new Mock<IAIConversationRepository>().Object, accessor.Object);

        var (items, total) = await service.GetSharedConversationsPagedAsync(0, 10);

        total.ShouldBe(0);
        items.ShouldBeEmpty();
        shares.Verify(
            r => r.GetSharedWithUserPagedAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    private static (AIConversationShareReadService Service, Mock<IAIConversationShareRepository> Shares) BuildService()
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
        return (service, shares);
    }
}

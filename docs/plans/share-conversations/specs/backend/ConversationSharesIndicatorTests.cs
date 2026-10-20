// STAGED SPEC — see ShareConversationTests.cs's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/, as part of SC-05).

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US2 AC5-6 (backend half) — the owner's tree-item entity-sign badge (SC-13) is driven
/// by a `HasActiveShares` field computed in the existing paged-list query, not a follow-up query
/// per conversation (ARCHITECTURE.md decision 6). This file covers that computation only; the
/// badge's rendering is specs/frontend/shared-indicator.spec.ts.
/// </summary>
public class ConversationSharesIndicatorTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    #region Happy path

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetConversationsPagedAsync_ConversationWithActiveShare_HasActiveSharesTrue()
    {
        // AC5 (backend half) — a conversation with at least one non-revoked share row reports
        // HasActiveShares: true in the paged list, without a per-row follow-up query.
    }

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetConversationsPagedAsync_ConversationWithOnlyRevokedShares_HasActiveSharesFalse()
    {
        // A conversation whose only shares are all revoked reports HasActiveShares: false — the
        // field reflects current exposure, not sharing history.
    }

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetConversationsPagedAsync_PinnedAndShared_BothFlagsTrueIndependently()
    {
        // AC6 (backend half) — IsPinned and HasActiveShares are independent fields; nothing in the
        // query makes one exclude the other. (The frontend renders both simultaneously — this spec
        // only guards that the data supports it.)
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetConversationsPagedAsync_NoShares_HasActiveSharesFalse()
    {
        // Regression guard: a conversation that's never been shared reports HasActiveShares: false,
        // not null/undefined — existing callers of this response model shouldn't need a null check.
    }

    #endregion
}

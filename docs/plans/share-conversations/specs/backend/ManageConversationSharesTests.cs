// STAGED SPEC — see ShareConversationTests.cs's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/, as part of SC-04/SC-07).

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US2 — See and manage who a conversation is shared with (backend half — listing and
/// revoking). Exercises SPEC.md's GET/DELETE /conversations/{id}/shares, which back both the
/// context panel's "Shared with" section (SC-14) and the owner's tree-item badge (SC-13, via the
/// separate `hasActiveShares` field covered by ConversationSharesIndicatorTests.cs).
/// </summary>
public class ManageConversationSharesTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();

    #region Happy path

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task GetSharesAsync_ConversationSharedWithTwo_ReturnsBothWithScopeAndDate()
    {
        // AC1
    }

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task RevokeShareAsync_ActiveShare_SetsDateRevoked()
    {
        // AC3 — soft-revoke, per ARCHITECTURE.md's "why soft-revoke" rationale: the row is kept, not
        // deleted.
    }

    [Fact(Skip = "Pending implementation — SC-04, SC-07")]
    public async Task RevokeShareAsync_ThenGetSharedConversationAsync_NoLongerResolves()
    {
        // AC4 — revocation is immediate: the next read through the recipient-side path (SC-07) fails
        // once DateRevoked is set. Spans both the owner-side revoke and the recipient-side read, so
        // this spec deliberately exercises both rather than assuming the wiring works from unit tests
        // of each half in isolation.
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task GetSharesAsync_NotOwned_ThrowsAndReturnsNothing()
    {
        // AC8
    }

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task RevokeShareAsync_AlreadyRevokedOrNeverExisted_ThrowsRatherThanNoOpSucceeding()
    {
        // AC9 — a second revoke is a real failure, not silently accepted.
    }

    #endregion
}

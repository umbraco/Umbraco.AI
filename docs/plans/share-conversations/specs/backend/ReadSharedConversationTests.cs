// STAGED SPEC — see ShareConversationTests.cs's header for why this lives here and where it moves
// (Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/, as part of SC-05/SC-07).

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US4 — Read a conversation shared with me (backend half). Exercises
/// GetSharedConversationAsync / GET /conversations/{id}/shared-view, including the Snapshot/Live
/// cutoff behavior and the graceful-degradation rule from ARCHITECTURE.md's data-model section.
/// ARCHITECTURE.md decision 1: this is a separate code path from AIConversationService's owner
/// methods — never calls GetOwnedOrThrowAsync.
/// </summary>
public class ReadSharedConversationTests
{
    private static readonly Guid RecipientKey = Guid.NewGuid();
    private static readonly Guid OwnerKey = Guid.NewGuid();

    #region Happy path

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetSharedConversationAsync_SnapshotScope_ReturnsMessagesUpToCutoffSequenceOnly()
    {
        // AC1 — every message whose Sequence <= the CutoffMessageId message's Sequence, nothing after.
    }

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetSharedConversationAsync_LiveScope_ReturnsMessagesAddedAfterSharing()
    {
        // AC2 — no ceiling for a Live share.
    }

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetSharedConversationAsync_ResourcesAndContextIds_ReturnedUnfiltered()
    {
        // SPEC.md — no per-viewer redaction (ARCHITECTURE.md decision 2): the same attachment list the
        // owner sees comes back as-is.
    }

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetSharedConversationAsync_CutoffMessageDeletedByRegenerate_ReturnsEveryRemainingMessage()
    {
        // AC5 — graceful degradation: TruncateAfterLastUserMessageAsync removed the anchor message,
        // the read falls back to everything that still exists rather than erroring.
    }

    // Real entry point, per bdd-specs rule 4.
    [Fact(Skip = "Pending implementation — SC-05, SC-07")]
    public async Task GetSharedConversationAsync_ResolvedFromDI_ReturnsSharedView()
    {
        // AC1, real-entry-point variant.
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-05")]
    public async Task GetSharedConversationAsync_NoActiveShareForActingUser_ThrowsNotFound()
    {
        // AC6 — a conversation that was never shared with the acting user 404s, same
        // not-found-vs-forbidden indistinguishability as the owner-only endpoints.
    }

    #endregion
}

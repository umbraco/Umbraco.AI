// STAGED SPEC — not yet in a real test project. Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit
// doesn't exist on this branch yet. Move this file to
// Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/ (matching the location of the
// existing AIConversationService*Tests.cs files) as part of task SC-04/SC-08. Naming, class shape,
// and assertion style (Moq + Shouldly, flat class + Method_Scenario_ExpectedResult, a BuildService()
// factory) match AIConversationServiceTruncateTests.cs in that same folder. The email-on-share
// behavior (AC7-8) has its own file, ShareNotificationEmailTests.cs, matching its own task (SC-06).

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US1 — Share a conversation with a colleague. Exercises the owner-side sharing
/// service methods from ARCHITECTURE.md decision 1/4 and SPEC.md's POST /conversations/{id}/shares.
/// The frontend picker only ever submits one recipient per call (ARCHITECTURE.md decision 7), but
/// the service method itself still accepts a collection (SPEC.md's `> ASSUMPTION`) — the
/// multi-recipient test below guards that capability even though no current UI path exercises it.
/// </summary>
public class ShareConversationTests
{
    private static readonly Guid OwnerKey = Guid.NewGuid();
    private static readonly Guid OtherOwnerKey = Guid.NewGuid();
    private static readonly Guid RecipientKey = Guid.NewGuid();
    private static readonly Guid RecipientKey2 = Guid.NewGuid();

    #region Happy path

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task ShareConversationAsync_SnapshotScope_SetsCutoffToCurrentLastMessage()
    {
        // AC1 — snapshot cutoff is resolved server-side from the conversation's current last message,
        // never accepted from the caller.
    }

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task ShareConversationAsync_MultipleRecipientKeysInOneCall_CreatesOneShareRowPerRecipient()
    {
        // SPEC.md's `> ASSUMPTION` — the service method (and the request body) still accepts N
        // recipientUserKeys in one call, even though STORIES.md AC2 describes the frontend doing
        // this as N separate picker invocations, not a batch. This guards the capability, not a
        // user-facing AC.
    }

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task ShareConversationAsync_LiveScope_LeavesCutoffMessageIdNull()
    {
        // AC3 — Live scope has no ceiling.
    }

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task ShareConversationAsync_RecipientAlreadyShared_UpdatesExistingRowInstead()
    {
        // AC4 — idempotent re-share updates ScopeMode/CutoffMessageId on the existing row rather than
        // duplicating it (unique SharedWithUserKey per ConversationId).
    }

    // Real entry point, per bdd-specs rule 4: resolves AIConversationShareService via the same DI
    // wiring the Composer registers, not a bare `new`. Exercised once here; the rest of this class's
    // specs may construct the service directly for isolation.
    [Fact(Skip = "Pending implementation — SC-04, SC-08")]
    public async Task ShareConversationAsync_ResolvedFromDI_SharesSuccessfully()
    {
        // AC1, real-entry-point variant.
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-04")]
    public async Task ShareConversationAsync_NotOwned_ThrowsAndCreatesNoShare()
    {
        // AC9 — not-found-vs-forbidden indistinguishable, same posture as every other write path
        // (ARCHITECTURE.md decision 1).
    }

    #endregion

    private static (object Service, Mock<IAIConversationRepository> Conversations, Mock<object> Shares) BuildService()
    {
        // Placeholder factory — mirrors AIConversationServiceTruncateTests.BuildService(). Fill in once
        // IAIConversationShareRepository (SC-01) exists; the second mock's type is a stand-in.
        throw new NotImplementedException("Wire up once SC-01/SC-02 land.");
    }
}

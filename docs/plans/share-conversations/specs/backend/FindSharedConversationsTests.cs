// STAGED SPEC — not yet in a real test project. Move to
// Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit/Conversations/ (matching
// AIConversationServiceTruncateTests.cs's location/style: Moq + Shouldly, flat class,
// Method_Scenario_ExpectedResult naming, a BuildService() factory) as part of task SC-07/SC-09.

using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// STORIES.md US3 — Find conversations shared with me (backend half). Exercises
/// GetSharedConversationsPagedAsync / GET /conversations/shared-with-me. The frontend half (sidebar
/// rendering) is specs/frontend/find-shared-conversations.spec.ts.
/// </summary>
public class FindSharedConversationsTests
{
    private static readonly Guid RecipientKey = Guid.NewGuid();
    private static readonly Guid OwnerKey = Guid.NewGuid();

    #region Happy path

    // Real entry point, per bdd-specs rule 4.
    [Fact(Skip = "Pending implementation — SC-07, SC-09")]
    public async Task GetSharedConversationsPagedAsync_ResolvedFromDI_ReturnsSharedConversation()
    {
        // AC1 (backend half) — an active share for the acting-as-recipient user appears in the page,
        // including the owner's display name (used for the frontend's hover-tooltip text, AC2).
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation — SC-07")]
    public async Task GetSharedConversationsPagedAsync_AfterRevoke_NoLongerIncludesIt()
    {
        // AC3 (backend half) — a revoked share drops out of the paged result.
    }

    #endregion
}

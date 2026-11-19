// GP-11 (AC1, AC2)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration;

/// <summary>
/// A Workspace chat with an agent the user lost stays readable but refuses new messages.
/// </summary>
public class ConversationAgentAccessTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T35")]
    public async Task OpenConversation_WithLostAgent_ShowsMessages()
    {
        // Given a saved conversation with an agent the user can no longer use
        // When  they open the conversation
        // Then  its messages are returned (AC1)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T35")]
    public async Task SendMessage_WithLostAgent_Returns403AgentNotAvailable()
    {
        // Given that conversation
        // When  the user sends a new message
        // Then  the stream endpoint returns 403 ProblemDetails titled "Agent not available" (AC2)
        await Task.CompletedTask;
    }

    #endregion
}

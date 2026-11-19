// GP-9 (AC6)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents.Selection;

/// <summary>
/// Automatic agent selection never picks an agent the user can't use.
/// </summary>
public class AgentSelectionUsageLimitTests
{
    #region Sad path

    [Fact(Skip = "Pending implementation: T18")]
    public async Task Select_BestMatchLimitedToOtherGroup_IsNotPicked()
    {
        // Given an agent limited to editors that best matches the request
        // When  Copilot picks an agent for a user not in editors
        // Then  the picked agent is not that agent (AC6)
        await Task.CompletedTask;
    }

    #endregion
}

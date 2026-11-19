// GP-10 (AC4); architecture guess 5
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Agent test runs check the agent limit against the person running the test.
/// </summary>
public class AgentTestRunUsageLimitTests
{
    #region Sad path

    [Fact(Skip = "Pending implementation: T19")]
    public async Task TestRun_ByUserOutsideGroups_IsRefused()
    {
        // Given a test of an agent limited to editors and a user with Uai.Test.Manage not in it
        // When  they run the test
        // Then  the agent run is refused (AC4)
        await Task.CompletedTask;
    }

    #endregion
}

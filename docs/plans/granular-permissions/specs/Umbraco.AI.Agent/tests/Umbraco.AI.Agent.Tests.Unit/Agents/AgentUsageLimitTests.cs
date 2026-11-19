// GP-9 (AC1, AC3, AC5, AC7), GP-10 (AC1-AC3)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// IAIAgentService hides and refuses limited agents for users outside their groups.
/// </summary>
public class AgentUsageLimitTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T16")]
    public async Task List_ForMember_IncludesLimitedAgent()
    {
        // Given an agent limited to editors and a user in editors
        // When  agents are listed
        // Then  the agent is included (GP-9 AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T16")]
    public async Task Run_ForMember_StartsTheRun()
    {
        // Given an agent limited to editors and a user in editors
        // When  RunAgentAsync is called
        // Then  the run starts (GP-10 AC1)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T16")]
    public async Task List_ForNonMember_ExcludesLimitedAgent()
    {
        // Given an agent limited to editors and a user not in it
        // When  agents are listed
        // Then  the agent isn't included (GP-9 AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T16")]
    public async Task GetByAlias_ForNonMemberWithoutManage_ReturnsNull()
    {
        // Given a user without Uai.Agent.Manage and not in editors
        // When  GetAgentByAliasAsync is called for the limited agent
        // Then  it returns null (GP-9 AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T16")]
    public async Task List_ForUsableButOutOfScopeAgent_ExcludesIt()
    {
        // Given an agent the user may use but out of scope for the surface
        // When  the surface lists agents
        // Then  the agent isn't included (GP-9 AC7, R13)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T16")]
    public async Task Run_ForNonMember_IsRefused()
    {
        // Given an agent limited to editors and a user not in it
        // When  any run or stream method is called (theory)
        // Then  an agent-not-available failure is returned (GP-10 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T16")]
    public async Task Run_ForManagerOutsideGroups_IsRefused()
    {
        // Given a user with Uai.Agent.Manage not in editors
        // When  they run the agent limited to editors
        // Then  an agent-not-available failure is returned (GP-10 AC3)
        await Task.CompletedTask;
    }

    #endregion
}

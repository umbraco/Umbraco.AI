// GP-2 (agents), GP-7 (AC4), GP-8 (AC4), GP-9 (AC2-AC5), GP-10 (AC2), GP-14 (AC3, AC4)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Api;

/// <summary>
/// Real entry point (bdd-specs rule 4): the agent Management API through the test server.
/// </summary>
public class AgentEndpointAccessTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T21")]
    public async Task PutAgent_WithAllowedUserGroupIds_RoundTrips()
    {
        // Given an AI section user with Uai.Agent.Manage
        // When  they PUT agents/{alias} with allowedUserGroupIds [editors key]
        // Then  GET agents/{alias} returns allowedUserGroupIds [editors key] (GP-7 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task GetAgents_IncludeRestricted_ForManager_ListsLimitedAgent()
    {
        // Given a user with Uai.Agent.Manage not in editors
        // When  they GET agents?includeRestricted=true
        // Then  the agent limited to editors is listed (GP-9 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task CreateAgent_WithManageAgents_Returns201()
    {
        // Given an AI section user with Uai.Agent.Manage
        // When  they POST agents with a valid body
        // Then  the response is 201 (GP-2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T21")]
    public async Task CreateAgent_WithoutManageAgents_Returns403()
    {
        // Given an AI section user without Uai.Agent.Manage
        // When  they POST agents
        // Then  the response is 403 (GP-2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task PutAgent_AddingSomeoneElsesGroup_Returns403()
    {
        // Given a non-admin with Uai.Agent.Manage in editors only
        // When  they PUT agents/{alias} adding translators
        // Then  the response is 403 (GP-8 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task GetAgents_ForNonMember_HidesLimitedAgent()
    {
        // Given an agent limited to editors and a user not in it
        // When  they GET agents
        // Then  the agent isn't listed (GP-9 AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task GetAgents_IncludeRestricted_WithoutManage_IsIgnored()
    {
        // Given a user without Uai.Agent.Manage and not in editors
        // When  they GET agents?includeRestricted=true
        // Then  the limited agent isn't listed (GP-9 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task GetAgentDetail_ForNonMemberWithoutManage_Returns404()
    {
        // Given a user without Uai.Agent.Manage and not in editors
        // When  they GET agents/{alias} for the limited agent
        // Then  the response is 404 (GP-9 AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task RunEndpoints_ForNonMember_Return403AgentNotAvailable()
    {
        // Given an agent limited to editors and a user not in it
        // When  they call run, stream, stream-agui, suggest-starters or its availability (theory)
        // Then  the response is 403 ProblemDetails titled "Agent not available" (GP-10 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task GetAgents_OnBehalfOfInaccessibleWorkspace_Returns403()
    {
        // Given a caller without access to workspace W
        // When  they GET agents?onBehalfOf=automate-workspace:W
        // Then  the response is 403 (GP-14 AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T21")]
    public async Task GetAgents_OnBehalfOfUnknownKindOrNoServiceAccount_Returns400()
    {
        // Given onBehalfOf=unknown:1, or a workspace with no service account (theory)
        // When  GET agents is called with it
        // Then  the response is 400 (GP-14 AC4)
        await Task.CompletedTask;
    }

    #endregion
}

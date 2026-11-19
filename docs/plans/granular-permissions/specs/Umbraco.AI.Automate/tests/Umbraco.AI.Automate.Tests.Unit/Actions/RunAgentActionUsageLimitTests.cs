// GP-15 (AC1, AC2), GP-16 (AC1-AC3)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

/// <summary>
/// The Run agent step checks the service account at publish and at run time.
/// </summary>
public class RunAgentActionUsageLimitTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T38")]
    public async Task Publish_WithUsableAgent_Succeeds()
    {
        // Given a Run agent step using an agent the service account can use
        // When  the automation is published
        // Then  publish validation passes (GP-15 AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T38")]
    public async Task Run_WithUsableAgent_Succeeds()
    {
        // Given a published automation whose service account can use its agent
        // When  the step runs
        // Then  the step succeeds (GP-16 AC1)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T38")]
    public async Task Publish_WithUnusableAgent_FailsNamingStepAndAgent()
    {
        // Given a Run agent step using an agent limited to a group the service account isn't in
        // When  the automation is published
        // Then  validation fails with an error naming the step and the agent (GP-15 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T38")]
    public async Task Run_AfterAccessLost_FailsWithAuthenticationCategory()
    {
        // Given the agent was limited after publishing to a group the service account isn't in
        // When  the step runs
        // Then  the step fails with category Authentication (GP-16 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T38")]
    public async Task Run_AfterAccessLost_GivesTheR18Reason()
    {
        // Given the same
        // When  the step runs
        // Then  the reason is "The workspace's service account can't use agent '<alias>'. Add one of its user groups to the agent, or pick another agent." (GP-16 AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T38")]
    public async Task Run_WithoutServiceAccount_DoesNotRunLimitedAgent()
    {
        // Given a workspace with no service account and a limited agent
        // When  the step runs
        // Then  the agent service is never called (GP-16 AC3)
        await Task.CompletedTask;
    }

    #endregion
}

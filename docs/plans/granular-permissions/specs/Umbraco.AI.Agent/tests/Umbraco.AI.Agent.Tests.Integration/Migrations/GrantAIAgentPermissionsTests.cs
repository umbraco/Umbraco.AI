// GP-6 (AC3)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Integration.Migrations;

/// <summary>
/// Umbraco.AI.Agent grants its own verb to AI section groups.
/// </summary>
public class GrantAIAgentPermissionsTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T15")]
    public async Task Migration_GivesAISectionGroups_ManageAgents()
    {
        // Given AI section groups already upgraded, then Umbraco.AI.Agent installed
        // When  the Agent migration runs
        // Then  every AI section group holds Uai.Agent.Manage (AC3)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T15")]
    public async Task ReRun_AddsNoDuplicateRows()
    {
        // Given the Agent migration already ran
        // When  it runs again
        // Then  each group holds Uai.Agent.Manage at most once (R7)
        await Task.CompletedTask;
    }

    #endregion
}

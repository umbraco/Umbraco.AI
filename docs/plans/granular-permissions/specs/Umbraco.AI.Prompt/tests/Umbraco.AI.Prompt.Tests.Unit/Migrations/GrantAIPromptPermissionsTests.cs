// GP-6 (AC4)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Migrations;

/// <summary>
/// Umbraco.AI.Prompt grants its own verb to AI section groups. (Prompt has no integration project: test the step's SQL against an in-memory SQLite database.)
/// </summary>
public class GrantAIPromptPermissionsTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T29")]
    public async Task Migration_GivesAISectionGroups_ManagePrompts()
    {
        // Given Umbraco.AI.Prompt is installed
        // When  its migration runs
        // Then  every AI section group holds Uai.Prompt.Manage (AC4)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T29")]
    public async Task ReRun_AddsNoDuplicateRows()
    {
        // Given the Prompt migration already ran
        // When  it runs again
        // Then  each group holds Uai.Prompt.Manage at most once (R7)
        await Task.CompletedTask;
    }

    #endregion
}

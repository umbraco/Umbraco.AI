// GP-6 (AC1, AC2, AC5, AC6, AC7)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Integration.Migrations;

/// <summary>
/// The core upgrade grants the 9 core verbs to AI section groups and the admin group, once, without undoing an admin's choice.
/// </summary>
public class GrantAIPermissionsToSectionGroupsTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T8")]
    public async Task Migration_GivesAISectionGroup_AllNineCoreVerbs()
    {
        // Given a group with the AI section before the upgrade
        // When  the Umbraco.AI migration runs
        // Then  the group holds all 9 core verbs (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T8")]
    public async Task Migration_GivesAdminGroup_AllNineCoreVerbs()
    {
        // Given the admin group's AI section row added in the same upgrade
        // When  the Umbraco.AI migration runs
        // Then  the admin group holds all 9 core verbs (AC2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T8")]
    public async Task ReRun_KeepsTurnedOffVerbOff()
    {
        // Given the migration ran and an admin turned off Uai.Connection.Manage on a group
        // When  the grant step runs again
        // Then  the group still lacks Uai.Connection.Manage (AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T8")]
    public async Task ReRun_AddsNoDuplicateRows()
    {
        // Given the migration already ran
        // When  the grant step runs again
        // Then  each group holds each AI verb at most once (AC6)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T8")]
    public async Task GroupGivenSectionLater_HasNoAIVerbs()
    {
        // Given a group given the AI section after the upgrade
        // When  it is first saved
        // Then  it holds no AI verbs (AC7)
        await Task.CompletedTask;
    }

    #endregion
}

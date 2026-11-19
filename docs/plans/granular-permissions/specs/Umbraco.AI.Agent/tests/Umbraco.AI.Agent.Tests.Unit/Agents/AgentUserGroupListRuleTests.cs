// GP-8 (AC1, AC2, AC4, AC6, AC7); spec guess 5
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Non-admins may only change an agent's group list within their own groups, on agents they can use.
/// </summary>
public class AgentUserGroupListRuleTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T17")]
    public async Task Save_ByAdmin_AnyGroups_Succeeds()
    {
        // Given an admin
        // When  they set an agent's groups to any groups and save
        // Then  the save succeeds (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T17")]
    public async Task Save_ByMemberAddingOwnGroup_Succeeds()
    {
        // Given a non-admin with Uai.Agent.Manage in editors and an unlimited agent
        // When  they add editors and save
        // Then  the save succeeds (AC2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T17")]
    public async Task Save_AddingSomeoneElsesGroup_IsRefused()
    {
        // Given a non-admin in editors only
        // When  they save the agent adding translators
        // Then  the save is refused (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T17")]
    public async Task Save_OnAgentTheyCannotUse_IsRefused()
    {
        // Given an agent limited to translators and a non-admin manager not in it
        // When  they change its group list
        // Then  the save is refused (R12, backs AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T17")]
    public async Task Save_EmptyingTheList_ByNonAdmin_IsRefused()
    {
        // Given an agent limited to editors and a non-admin manager in editors
        // When  they remove editors and save
        // Then  the save is refused (AC6)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T17")]
    public async Task RefusedSave_KeepsOtherChangesOut()
    {
        // Given a non-admin in editors only
        // When  they save adding translators and renaming the agent
        // Then  the agent keeps its old name (AC7)
        await Task.CompletedTask;
    }

    #endregion
}

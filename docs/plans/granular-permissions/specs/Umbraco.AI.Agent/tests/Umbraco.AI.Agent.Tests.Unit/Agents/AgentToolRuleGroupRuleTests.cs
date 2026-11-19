// GP-18 (AC1, AC2, AC5-AC8); SPEC R22, R23
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Non-admins may only change an agent's tool rules for groups they belong to.
/// </summary>
public class AgentToolRuleGroupRuleTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T46")]
    public async Task Save_ByAdmin_ChangingAnyGroupsRules_Succeeds()
    {
        // Given an admin and an agent with tool rules for translators
        // When  they change the translators rules and save
        // Then  the save succeeds (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T46")]
    public async Task Save_ByMember_ChangingOwnGroupsRules_Succeeds()
    {
        // Given a non-admin with Uai.Agent.Manage in editors
        // When  they change the editors rules and save
        // Then  the save succeeds (AC2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T46")]
    public async Task Save_ChangingOtherGroupsRules_IsRefused()
    {
        // Given a non-admin in editors only and an agent with tool rules for translators
        // When  they save changed translators rules
        // Then  AIPermissionDeniedException is thrown (AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T46")]
    public async Task Save_RemovingOtherGroupsRules_IsRefused()
    {
        // Given the same non-admin and agent
        // When  they save without the translators entry
        // Then  AIPermissionDeniedException is thrown (AC6)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T46")]
    public async Task RefusedSave_KeepsOtherChangesOut()
    {
        // Given the same non-admin and agent
        // When  they save changed translators rules and a new agent name
        // Then  the agent keeps its old name (AC7)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T46")]
    public async Task Rollback_ChangingOtherGroupsRules_ByNonAdmin_IsRefused()
    {
        // Given translators rules changed by an admin since an older version, and a non-admin not in translators
        // When  they roll the agent back to that version
        // Then  AIPermissionDeniedException is thrown (AC8)
        await Task.CompletedTask;
    }

    #endregion
}

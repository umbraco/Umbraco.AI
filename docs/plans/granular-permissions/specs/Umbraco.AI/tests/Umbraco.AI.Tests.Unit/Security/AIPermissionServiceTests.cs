// GP-1 (AC3), GP-2 (AC1, AC4); rules R1, R9, R10, R14
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Security;

/// <summary>
/// AIPermissionService decides rights from the current user's groups and who may use an agent or prompt.
/// </summary>
public class AIPermissionServiceTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T1")]
    public async Task HasPermission_WhenAnyGroupHoldsTheVerb_ReturnsTrue()
    {
        // Given a user in two groups, one holding Uai.Context.Manage
        // When  HasPermission(Uai.Context.Manage) is called
        // Then  it returns true
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task HasPermission_ReadsSavedVerb_FromReadOnlyUserGroupPermissions()
    {
        // Given a group saved with Uai.Context.Manage ticked
        // When  the group is read through IReadOnlyUserGroup.Permissions
        // Then  Permissions contains Uai.Context.Manage (GP-1 AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task CanUse_WhenListIsEmpty_ReturnsTrue()
    {
        // Given an empty allowed group list
        // When  CanUse is called for any user
        // Then  it returns true (R9)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task CanUse_WhenListIsNull_ReturnsTrue()
    {
        // Given a null allowed group list
        // When  CanUse is called for any user
        // Then  it returns true (R9)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task CanUse_WhenUserSharesAGroup_ReturnsTrue()
    {
        // Given a list of [editors] and a user in editors
        // When  CanUse is called
        // Then  it returns true (R10)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T1")]
    public async Task HasPermission_WhenNoGroupHoldsTheVerb_ReturnsFalse()
    {
        // Given a user whose groups hold every AI verb except Uai.Connection.Manage
        // When  HasPermission(Uai.Connection.Manage) is called
        // Then  it returns false
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task HasPermission_ForAdminGroupWithoutTheVerb_ReturnsFalse()
    {
        // Given a user in the admin group, which lacks Uai.Connection.Manage
        // When  HasPermission(Uai.Connection.Manage) is called
        // Then  it returns false (no admin bypass, R1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task CanUse_WhenUserSharesNoGroup_ReturnsFalse()
    {
        // Given a list of [editors] and a user in translators only
        // When  CanUse is called
        // Then  it returns false (R10)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T1")]
    public async Task CanUse_WhenOnlyGroupWasDeleted_ReturnsFalse()
    {
        // Given a list holding only the key of a deleted group
        // When  CanUse is called for any user
        // Then  it returns false (R14)
        await Task.CompletedTask;
    }

    #endregion
}

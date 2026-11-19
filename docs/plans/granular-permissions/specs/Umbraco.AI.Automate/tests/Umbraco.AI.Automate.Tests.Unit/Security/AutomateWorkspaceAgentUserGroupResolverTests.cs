// GP-14 (AC1-AC4)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Automate.Tests.Unit.Security;

/// <summary>
/// The automate-workspace resolver answers with the workspace service account's groups.
/// </summary>
public class AutomateWorkspaceAgentUserGroupResolverTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T37")]
    public async Task Resolve_ReturnsServiceAccountGroups()
    {
        // Given a workspace whose service account is in automation, and a caller with access
        // When  automate-workspace:W is resolved
        // Then  the result holds [automation] (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T37")]
    public async Task Resolve_DoesNotUseCallersGroups()
    {
        // Given a builder in editors and a service account not in editors
        // When  automate-workspace:W is resolved
        // Then  the result doesn't hold editors (AC2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T37")]
    public async Task Resolve_CallerWithoutWorkspaceAccess_IsForbidden()
    {
        // Given a caller without access to W
        // When  automate-workspace:W is resolved
        // Then  the result is forbidden (AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T37")]
    public async Task Resolve_WorkspaceWithoutServiceAccount_IsNoUser()
    {
        // Given a workspace with no service account
        // When  automate-workspace:W is resolved
        // Then  the result is no-user (AC4)
        await Task.CompletedTask;
    }

    #endregion
}

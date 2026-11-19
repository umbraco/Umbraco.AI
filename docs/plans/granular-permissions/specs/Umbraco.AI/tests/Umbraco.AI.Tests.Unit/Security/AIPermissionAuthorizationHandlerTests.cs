// GP-2 (AC4, AC6)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Security;

/// <summary>
/// Each right's policy needs both the AI section and the right's verb.
/// </summary>
public class AIPermissionAuthorizationHandlerTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T2")]
    public async Task Policy_WithSectionAndRight_Succeeds()
    {
        // Given a user with the AI section and Uai.Connection.Manage
        // When  the Manage connections policy is evaluated
        // Then  it succeeds
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T2")]
    public async Task Policy_WithSectionButNotRight_Fails()
    {
        // Given a user with the AI section but not Uai.Connection.Manage
        // When  the Manage connections policy is evaluated
        // Then  it fails (GP-2 AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T2")]
    public async Task Policy_WithRightButNotSection_Fails()
    {
        // Given a user with Uai.Connection.Manage but not the AI section
        // When  the Manage connections policy is evaluated
        // Then  it fails (GP-2 AC6)
        await Task.CompletedTask;
    }

    #endregion
}

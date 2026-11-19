// GP-3 (AC1-AC4); PLAN guess 1
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management.Versioning;

/// <summary>
/// Rollback needs the manage right of the area that owns the entity type.
/// </summary>
public class RollbackAuthorizationTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T6")]
    public async Task ProfileRollback_WithManageProfiles_Returns200()
    {
        // Given an AI section user with Uai.Profile.Manage and a profile with an older version
        // When  they POST versions/{profile type}/{id}/{version}/rollback
        // Then  the response is 200 (AC1)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T6")]
    public async Task ProfileRollback_WithoutManageProfiles_Returns403()
    {
        // Given an AI section user with every AI right except Uai.Profile.Manage
        // When  they roll back a profile
        // Then  the response is 403 (AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T6")]
    public async Task ProfileRollback_WithoutManageProfiles_LeavesCurrentVersion()
    {
        // Given an AI section user without Uai.Profile.Manage
        // When  they roll back a profile
        // Then  the profile's current version is unchanged (AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T6")]
    public async Task AgentRollback_WithOnlyManageProfiles_Returns403()
    {
        // Given an AI section user with Uai.Profile.Manage but not Uai.Agent.Manage
        // When  they roll back an agent
        // Then  the response is 403 (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T6")]
    public async Task Rollback_WhenAdapterThrowsPermissionDenied_Returns403()
    {
        // Given an AI section user with Uai.Agent.Manage and an agent adapter whose rollback throws AIPermissionDeniedException
        // When  they roll the agent back
        // Then  the response is 403 (GP-8 AC8, GP-18 AC8)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T6")]
    public async Task UnmappedEntityTypeRollback_Returns403()
    {
        // Given an AI section user with every AI right and an entity type with no mapped right
        // When  they roll it back
        // Then  the response is 403 (PLAN guess 1)
        await Task.CompletedTask;
    }

    #endregion
}

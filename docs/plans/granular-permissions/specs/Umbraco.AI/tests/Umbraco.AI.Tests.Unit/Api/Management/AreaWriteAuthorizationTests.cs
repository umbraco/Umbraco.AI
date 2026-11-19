// GP-2 (AC1-AC7)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Every AI write endpoint is guarded by its area's policy; reads stay as they are (#509). Real entry point: controller attributes plus T7's live requests.
/// </summary>
public class AreaWriteAuthorizationTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T3")]
    public async Task ConnectionCreate_WithManageConnections_Returns201()
    {
        // Given an AI section user with Uai.Connection.Manage
        // When  they POST connections with a valid body
        // Then  the response is 201 (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T3, T4, T5")]
    public async Task EachWriteEndpoint_RequiresItsAreaPolicy()
    {
        // Given each write endpoint listed in SPEC.md (theory over controller types)
        // When  its authorization policies are read
        // Then  they contain that area's policy (AC2)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T3")]
    public async Task ProfileList_WithNoAIRights_Returns200()
    {
        // Given an AI section user with no AI rights
        // When  they GET profiles
        // Then  the response is 200 (AC3)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T3")]
    public async Task ConnectionCreate_WithoutManageConnections_Returns403()
    {
        // Given an AI section user with every AI right except Uai.Connection.Manage
        // When  they POST connections
        // Then  the response is 403 (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T3")]
    public async Task ConnectionDelete_WithoutManageConnections_LeavesConnection()
    {
        // Given an AI section user without Uai.Connection.Manage and an existing connection
        // When  they DELETE connections/{alias}
        // Then  the connection still exists (AC5)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T3")]
    public async Task ConnectionCreate_WithRightButNoSection_Returns403()
    {
        // Given a user with Uai.Connection.Manage but not the AI section
        // When  they POST connections
        // Then  the response is 403 (AC6)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T5")]
    public async Task AuditLogDelete_WithViewLogsOnly_Returns403()
    {
        // Given an AI section user with Uai.AuditLog.View but not Uai.Settings.Manage
        // When  they call the audit log delete endpoint
        // Then  the response is 403 (AC7, spec guess 1)
        await Task.CompletedTask;
    }

    #endregion
}

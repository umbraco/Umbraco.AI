// GP-4 (AC1-AC5)
//
// STAGED SPEC: lives in docs/plans/granular-permissions/specs/ until its task moves it to the
// path below this folder and makes it pass. Bodies hold the Given/When/Then; the builder fills them.

using Shouldly;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Audit log and analytics reads need their view rights.
/// </summary>
public class MonitoringAuthorizationTests
{
    #region Happy path

    [Fact(Skip = "Pending implementation: T5")]
    public async Task AuditLogList_WithViewLogs_Returns200()
    {
        // Given an AI section user with Uai.AuditLog.View
        // When  they call the audit log list endpoint
        // Then  the response is 200 (AC1)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T5")]
    public async Task UsageSummary_WithViewAnalytics_Returns200()
    {
        // Given an AI section user with Uai.Analytics.View
        // When  they call the usage summary endpoint
        // Then  the response is 200 (AC2)
        await Task.CompletedTask;
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending implementation: T5")]
    public async Task AuditLogList_WithoutViewLogs_Returns403()
    {
        // Given an AI section user without Uai.AuditLog.View
        // When  they call the audit log list endpoint
        // Then  the response is 403 (AC3)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T5")]
    public async Task AuditLogDetail_WithoutViewLogs_Returns403()
    {
        // Given an AI section user without Uai.AuditLog.View
        // When  they call the audit log detail endpoint
        // Then  the response is 403 (AC4)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Pending implementation: T5")]
    public async Task AnalyticsEndpoints_WithoutViewAnalytics_Return403()
    {
        // Given an AI section user without Uai.Analytics.View (theory: summary, time series, breakdown)
        // When  they call the endpoint
        // Then  the response is 403 (AC5)
        await Task.CompletedTask;
    }

    #endregion
}

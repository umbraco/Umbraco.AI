// GP-4 (AC1-AC5)

using Umbraco.AI.Core.Security;
using Umbraco.AI.Web.Api.Management.Analytics.Usage.Controllers;
using Umbraco.AI.Web.Api.Management.AuditLog.Controllers;

using static Umbraco.AI.Tests.Unit.Api.Management.AreaWriteAuthorizationTests;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Audit log and analytics reads need their view rights. Like <see cref="AreaWriteAuthorizationTests"/>, these
/// read the policy off the real controller and evaluate it with the real handlers.
/// </summary>
public class MonitoringAuthorizationTests
{
    public static TheoryData<Type> AnalyticsControllers => new()
    {
        { typeof(UsageSummaryController) },
        { typeof(UsageTimeSeriesController) },
        { typeof(UsageBreakdownController) },
    };

    #region Happy path

    [Fact]
    public async Task AuditLogList_WithViewLogs_Returns200()
    {
        // Given an AI section user with Uai.AuditLog.View
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [AIPermissions.ViewAuditLogs]);

        // When  they call the audit log list endpoint
        var allowed = await IsAllowedAsync(authorizationService, typeof(AllAuditLogController));

        // Then  the response is 200 (AC1)
        allowed.ShouldBeTrue();
    }

    [Fact]
    public async Task UsageSummary_WithViewAnalytics_Returns200()
    {
        // Given an AI section user with Uai.Analytics.View
        var authorizationService = CreateAuthorizationService(hasSection: true, verbs: [AIPermissions.ViewAnalytics]);

        // When  they call the usage summary endpoint
        var allowed = await IsAllowedAsync(authorizationService, typeof(UsageSummaryController));

        // Then  the response is 200 (AC2)
        allowed.ShouldBeTrue();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task AuditLogList_WithoutViewLogs_Returns403()
    {
        // Given an AI section user without Uai.AuditLog.View
        var authorizationService = CreateAuthorizationService(
            hasSection: true,
            verbs: AllVerbs.Where(v => v != AIPermissions.ViewAuditLogs).ToArray());

        // When  they call the audit log list endpoint
        var allowed = await IsAllowedAsync(authorizationService, typeof(AllAuditLogController));

        // Then  the response is 403 (AC3)
        allowed.ShouldBeFalse();
    }

    [Fact]
    public async Task AuditLogDetail_WithoutViewLogs_Returns403()
    {
        // Given an AI section user without Uai.AuditLog.View
        var authorizationService = CreateAuthorizationService(
            hasSection: true,
            verbs: AllVerbs.Where(v => v != AIPermissions.ViewAuditLogs).ToArray());

        // When  they call the audit log detail endpoint
        var allowed = await IsAllowedAsync(authorizationService, typeof(ByIdAuditLogController));

        // Then  the response is 403 (AC4)
        allowed.ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(AnalyticsControllers))]
    public async Task AnalyticsEndpoints_WithoutViewAnalytics_Return403(Type controllerType)
    {
        // Given an AI section user without Uai.Analytics.View (theory: summary, time series, breakdown)
        var authorizationService = CreateAuthorizationService(
            hasSection: true,
            verbs: AllVerbs.Where(v => v != AIPermissions.ViewAnalytics).ToArray());

        // When  they call the endpoint
        var allowed = await IsAllowedAsync(authorizationService, controllerType);

        // Then  the response is 403 (AC5)
        allowed.ShouldBeFalse();
    }

    #endregion
}

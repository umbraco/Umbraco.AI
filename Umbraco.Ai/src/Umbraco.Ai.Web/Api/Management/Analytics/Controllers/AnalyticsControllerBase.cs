using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Analytics.Controllers;

/// <summary>
/// Base controller for Analytics management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Analytics.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Analytics.RouteSegment)]
public abstract class AnalyticsControllerBase : UmbracoAiCoreManagementControllerBase
{
}

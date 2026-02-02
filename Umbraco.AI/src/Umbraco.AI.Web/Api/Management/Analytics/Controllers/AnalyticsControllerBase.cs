using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Analytics.Controllers;

/// <summary>
/// Base controller for Analytics management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Analytics.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Analytics.RouteSegment)]
public abstract class AnalyticsControllerBase : UmbracoAICoreManagementControllerBase
{
}

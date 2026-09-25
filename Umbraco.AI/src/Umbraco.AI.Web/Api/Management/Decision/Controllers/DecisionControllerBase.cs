using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Decision.Controllers;

/// <summary>
/// Base controller for Decision management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Decision.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Decision.RouteSegment)]
[TypeFilter(typeof(DecisionCapabilityGateFilter))]
public abstract class DecisionControllerBase : UmbracoAICoreManagementControllerBase
{
}

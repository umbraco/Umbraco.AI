using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.ToolScope.Controllers;

/// <summary>
/// Base controller for Tool Scope management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.ToolScope.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.ToolScope.RouteSegment)]
public abstract class ToolScopeControllerBase : UmbracoAICoreManagementControllerBase
{
}

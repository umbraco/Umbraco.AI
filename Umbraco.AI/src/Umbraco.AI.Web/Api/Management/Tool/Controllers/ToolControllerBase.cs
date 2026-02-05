using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Tool.Controllers;

/// <summary>
/// Base controller for Tool Scope management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Tools.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Tools.RouteSegment)]
public abstract class ToolControllerBase : UmbracoAICoreManagementControllerBase
{
}

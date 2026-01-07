using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.ContextResourceTypes.Controllers;

/// <summary>
/// Base controller for Context management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.ContextResourceTypes.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.ContextResourceTypes.RouteSegment)]
public abstract class ResourceTypeControllerBase : UmbracoAiCoreManagementControllerBase
{ }

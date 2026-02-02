using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.ContextResourceTypes.Controllers;

/// <summary>
/// Base controller for Context management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.ContextResourceTypes.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.ContextResourceTypes.RouteSegment)]
public abstract class ContextResourceTypeControllerBase : UmbracoAICoreManagementControllerBase
{ }

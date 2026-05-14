using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.PropertyValueOperation.Controllers;

/// <summary>
/// Base controller for property value operation management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.PropertyValueOperation.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.PropertyValueOperation.RouteSegment)]
public abstract class PropertyValueOperationControllerBase : UmbracoAICoreManagementControllerBase
{
}

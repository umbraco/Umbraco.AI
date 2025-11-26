using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Provider.Controllers;

/// <summary>
/// Base controller for Provider management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Provider.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Provider.RouteSegment)]
public abstract class ProviderControllerBase : UmbracoAiManagementControllerBase
{
}

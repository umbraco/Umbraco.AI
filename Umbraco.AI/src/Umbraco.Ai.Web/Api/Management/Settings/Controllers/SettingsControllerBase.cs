using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Settings.Controllers;

/// <summary>
/// Base controller for Settings management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Settings.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Settings.RouteSegment)]
public abstract class SettingsControllerBase : UmbracoAiCoreManagementControllerBase
{
}

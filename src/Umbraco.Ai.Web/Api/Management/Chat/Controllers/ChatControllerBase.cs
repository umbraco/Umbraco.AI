using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Web.Api;
using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Web.Api.Management.Chat.Controllers;

/// <summary>
/// Base controller for Chat management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Chat.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Chat.RouteSegment)]
public abstract class ChatControllerBase : UmbracoAiCoreManagementControllerBase
{
}

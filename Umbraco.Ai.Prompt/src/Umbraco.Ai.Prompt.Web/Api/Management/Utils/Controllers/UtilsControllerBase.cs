using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Utils.Controllers;

/// <summary>
/// Base controller for utils endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Utils.GroupName)]
[UmbracoAiVersionedManagementApiRoute(Constants.ManagementApi.Feature.Utils.RouteSegment)]
public abstract class UtilsControllerBase : UmbracoAiPromptManagementControllerBase
{ }

using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Prompt.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Prompt.Web.Api.Management.Utils.Controllers;

/// <summary>
/// Base controller for utils endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Utils.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Utils.RouteSegment)]
public abstract class UtilsControllerBase : UmbracoAIPromptManagementControllerBase
{ }

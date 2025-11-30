using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Prompt.Web.Api.Management.Common.Controllers;
using Umbraco.Ai.Prompt.Web.Api.Management.Common.Routing;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Controllers;

/// <summary>
/// Base controller for prompt-specific endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Api.Constants.ManagementApi.Feature.Prompt.GroupName)]
[UmbracoAiPromptVersionedManagementApiRoute(Api.Constants.ManagementApi.Feature.Prompt.RouteSegment)]
public abstract class PromptControllerBase : UmbracoAiPromptManagementControllerBase
{
}

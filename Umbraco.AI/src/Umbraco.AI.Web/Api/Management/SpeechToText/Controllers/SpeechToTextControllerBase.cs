using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.SpeechToText.Controllers;

/// <summary>
/// Base controller for Speech-to-Text management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.SpeechToText.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.SpeechToText.RouteSegment)]
public abstract class SpeechToTextControllerBase : UmbracoAICoreManagementControllerBase
{
}

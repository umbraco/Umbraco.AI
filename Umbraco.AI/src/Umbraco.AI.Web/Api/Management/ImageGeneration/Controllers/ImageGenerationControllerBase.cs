using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Filters;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.ImageGeneration.Controllers;

/// <summary>
/// Base controller for Image Generation management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.ImageGeneration.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.ImageGeneration.RouteSegment)]
[AICapabilityGate(AICapability.ImageGeneration)]
public abstract class ImageGenerationControllerBase : UmbracoAICoreManagementControllerBase
{
}

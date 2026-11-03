using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Filters;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.Extraction.Controllers;

/// <summary>
/// Base controller for Extraction management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.Extraction.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.Extraction.RouteSegment)]
[AICapabilityGate(AICapability.Extraction)]
public abstract class ExtractionControllerBase : UmbracoAICoreManagementControllerBase
{
}

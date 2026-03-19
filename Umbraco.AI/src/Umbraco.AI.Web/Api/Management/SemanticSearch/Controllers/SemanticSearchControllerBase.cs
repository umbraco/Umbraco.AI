using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.SemanticSearch.Controllers;

/// <summary>
/// Base controller for Semantic Search management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.SemanticSearch.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.SemanticSearch.RouteSegment)]
public abstract class SemanticSearchControllerBase : UmbracoAICoreManagementControllerBase
{
}

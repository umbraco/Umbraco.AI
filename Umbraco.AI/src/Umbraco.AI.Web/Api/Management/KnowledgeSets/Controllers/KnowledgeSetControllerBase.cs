using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;

namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Controllers;

/// <summary>
/// Base controller for Knowledge Set management API endpoints.
/// </summary>
[ApiExplorerSettings(GroupName = Constants.ManagementApi.Feature.KnowledgeSets.GroupName)]
[UmbracoAIVersionedManagementApiRoute(Constants.ManagementApi.Feature.KnowledgeSets.RouteSegment)]
public abstract class KnowledgeSetControllerBase : UmbracoAICoreManagementControllerBase
{ }

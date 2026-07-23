using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Agent.Conversations.Web.Api.Management;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream.Controllers;

/// <summary>
/// Base controller for the Copilot Workspace persisted stream/file endpoints. Binds to the single
/// <c>ai-copilot-workspace-management</c> OpenAPI document under the <c>Stream</c> area at the
/// <c>conversations</c> route segment, and requires the Copilot Workspace section policy on top of the
/// backoffice-access policy inherited from <see cref="UmbracoAIManagementControllerBase"/> (F-SEC).
/// </summary>
/// <remarks>
/// A sibling of the Conversations CRUD base in the same OpenAPI document. These stream/file controllers
/// live in the host assembly, so they carry the product binding as compile-time attributes directly —
/// unlike the reusable Conversations CRUD controllers, which receive the same binding at runtime via the
/// host's application-model convention (see <c>CopilotWorkspaceConversationsApiConvention</c>).
/// </remarks>
[MapToApi(CopilotWorkspaceConstants.ManagementApi.ApiName)]
[JsonOptionsName(CopilotWorkspaceConstants.ManagementApi.ApiName)]
[Authorize(Policy = CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace)]
[ApiExplorerSettings(GroupName = CopilotWorkspaceConstants.ManagementApi.Stream.GroupName)]
[UmbracoAIVersionedManagementApiRoute(ConversationsManagementApiConstants.Conversations.RouteSegment)]
public abstract class CopilotWorkspaceStreamControllerBase : UmbracoAIManagementControllerBase
{
}

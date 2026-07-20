using Microsoft.AspNetCore.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for the Copilot Workspace management API. Binds to the single
/// <c>ai-copilot-workspace-management</c> OpenAPI document and requires the Copilot Workspace section
/// policy on top of the backoffice-access policy inherited from
/// <see cref="UmbracoAIManagementControllerBase"/>. Because this gate sits on the CRUD controllers —
/// not just the UI — it protects the stored corpus, closing the section-gate-vs-stored-data gap (F-SEC).
/// </summary>
[MapToApi(CopilotWorkspaceConstants.ManagementApi.ApiName)]
[JsonOptionsName(CopilotWorkspaceConstants.ManagementApi.ApiName)]
[Authorize(Policy = CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace)]
public abstract class CopilotWorkspaceManagementControllerBase : UmbracoAIManagementControllerBase
{
}

using Microsoft.AspNetCore.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.Cms.Api.Management.Security.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Authorization;

/// <summary>
/// Authorizes access to Copilot Workspace endpoints by requiring the acting user to have the
/// Copilot Workspace section in their allowed sections.
/// </summary>
internal sealed class CopilotWorkspaceSectionAuthorizationHandler
    : MustSatisfyRequirementAuthorizationHandler<CopilotWorkspaceSectionRequirement>
{
    private readonly IAuthorizationHelper _authorizationHelper;

    public CopilotWorkspaceSectionAuthorizationHandler(IAuthorizationHelper authorizationHelper)
        => _authorizationHelper = authorizationHelper;

    protected override Task<bool> IsAuthorized(AuthorizationHandlerContext context, CopilotWorkspaceSectionRequirement requirement)
    {
        var allowed = _authorizationHelper.TryGetUmbracoUser(context.User, out IUser? user)
                      && user.AllowedSections.Contains(CopilotWorkspaceConstants.Sections.CopilotWorkspace);
        return Task.FromResult(allowed);
    }
}

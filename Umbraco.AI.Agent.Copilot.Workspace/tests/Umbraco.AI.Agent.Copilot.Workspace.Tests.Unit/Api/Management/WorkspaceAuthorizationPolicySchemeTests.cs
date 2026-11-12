using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.AI.Agent.Copilot.Workspace.Web.Authorization;
using Shouldly;
using Xunit;
using WorkspaceWebBuilder =Umbraco.AI.Agent.Copilot.Workspace.Web.Configuration.UmbracoBuilderExtensions;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management;

/// <summary>
/// Matches the CMS 19 backoffice policy shape: OpenIddict validation scheme plus the backoffice cookie scheme.
/// </summary>
public class WorkspaceAuthorizationPolicySchemeTests
{
    private static AuthorizationPolicy GetPolicy()
    {
        var options = new AuthorizationOptions();
        WorkspaceWebBuilder.ConfigureAuthorization(options);
        return options.GetPolicy(CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace).ShouldNotBeNull();
    }

    [Fact]
    public void SectionAccessCopilotWorkspace_AuthenticatesWithOpenIddictAndBackOfficeCookie()
    {
        GetPolicy().AuthenticationSchemes.ShouldBe(
            [OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Cms.Core.Constants.Security.BackOfficeAuthenticationType],
            ignoreOrder: true);
    }

    [Fact]
    public void SectionAccessCopilotWorkspace_RequiresWorkspaceSection()
    {
        GetPolicy().Requirements.ShouldContain(r => r is CopilotWorkspaceSectionRequirement);
    }
}

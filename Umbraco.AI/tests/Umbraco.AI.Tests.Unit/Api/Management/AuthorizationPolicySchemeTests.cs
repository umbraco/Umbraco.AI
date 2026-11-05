using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// CMS 19 authenticates the backoffice with a cookie; its own policies add both the OpenIddict validation
/// scheme and the backoffice cookie scheme. Our policy must match so it never depends on another policy
/// on the same endpoint to supply the cookie scheme.
/// </summary>
public class AuthorizationPolicySchemeTests
{
    private static AuthorizationPolicy GetPolicy()
    {
        var options = new AuthorizationOptions();
        AIAuthorizationPolicyConfiguration.Configure(options);
        return options.GetPolicy(AIAuthorizationPolicies.SectionAccessAI).ShouldNotBeNull();
    }

    [Fact]
    public void SectionAccessAI_AuthenticatesWithOpenIddictAndBackOfficeCookie()
    {
        GetPolicy().AuthenticationSchemes.ShouldBe(
            [OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Cms.Core.Constants.Security.BackOfficeAuthenticationType],
            ignoreOrder: true);
    }

    [Fact]
    public void SectionAccessAI_RequiresAISection()
    {
        GetPolicy().Requirements.ShouldContain(r => r is AISectionRequirement);
    }
}

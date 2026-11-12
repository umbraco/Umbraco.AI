using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;

namespace Umbraco.AI.Web.Authorization;

internal static class AIAuthorizationPolicyConfiguration
{
    /// <summary>
    /// Registers the AI authorization policies. Mirrors how the CMS builds its own backoffice policies:
    /// both the OpenIddict validation scheme (bearer tokens) and the backoffice cookie scheme.
    /// </summary>
    internal static void Configure(AuthorizationOptions options)
        => options.AddPolicy(AIAuthorizationPolicies.SectionAccessAI, policy =>
        {
            policy.AddAuthenticationSchemes(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                Cms.Core.Constants.Security.BackOfficeAuthenticationType);
            policy.Requirements.Add(new AISectionRequirement());
        });
}

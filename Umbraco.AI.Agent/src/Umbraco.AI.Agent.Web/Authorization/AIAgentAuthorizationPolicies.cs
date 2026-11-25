using Microsoft.AspNetCore.Authorization;

using Umbraco.AI.Agent.Core.Security;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Agent.Web.Authorization;

/// <summary>
/// Authorization policy names for the agent endpoints.
/// </summary>
public static class AIAgentAuthorizationPolicies
{
    /// <summary>
    /// Create, update and delete agents. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageAgentsAI = nameof(ManageAgentsAI);

    /// <summary>Registers the agent policies with core's permission policy builder.</summary>
    internal static void Register(AuthorizationOptions options)
        => AIAuthorizationPolicies.AddPermissionPolicy(options, ManageAgentsAI, AIAgentPermissions.Manage);
}

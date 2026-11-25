using Microsoft.AspNetCore.Authorization;

using Umbraco.AI.Prompt.Core.Security;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Prompt.Web.Authorization;

/// <summary>
/// Authorization policy names for the prompt endpoints.
/// </summary>
public static class AIPromptAuthorizationPolicies
{
    /// <summary>
    /// Create, update and delete prompts. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManagePromptsAI = nameof(ManagePromptsAI);

    /// <summary>Registers the prompt policies with core's permission policy builder.</summary>
    internal static void Register(AuthorizationOptions options)
        => AIAuthorizationPolicies.AddPermissionPolicy(options, ManagePromptsAI, AIPromptPermissions.Manage);
}

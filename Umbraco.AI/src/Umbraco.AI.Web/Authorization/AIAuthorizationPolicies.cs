using Microsoft.AspNetCore.Authorization;

using OpenIddict.Validation.AspNetCore;

using Umbraco.AI.Core.Security;

namespace Umbraco.AI.Web.Authorization;

/// <summary>
/// Provides constants for AI-related authorization policies.
/// Used to define and enforce authorization policies in the AI-related sections of the application.
/// </summary>
public static class AIAuthorizationPolicies
{
    /// <summary>
    /// Represents the policy name for accessing the AI-related section.
    /// </summary>
    public const string SectionAccessAI = nameof(SectionAccessAI);

    /// <summary>
    /// Create, update, delete and test connections. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageConnectionsAI = nameof(ManageConnectionsAI);

    /// <summary>
    /// Create, update and delete profiles. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageProfilesAI = nameof(ManageProfilesAI);

    /// <summary>
    /// Create, update and delete contexts. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageContextsAI = nameof(ManageContextsAI);

    /// <summary>
    /// Create, update and delete knowledge sets. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageKnowledgeSetsAI = nameof(ManageKnowledgeSetsAI);

    /// <summary>
    /// Create, update and delete guardrails. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageGuardrailsAI = nameof(ManageGuardrailsAI);

    /// <summary>
    /// Change AI settings. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageSettingsAI = nameof(ManageSettingsAI);

    /// <summary>
    /// View audit logs. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ViewAuditLogsAI = nameof(ViewAuditLogsAI);

    /// <summary>
    /// View usage analytics. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ViewAnalyticsAI = nameof(ViewAnalyticsAI);

    /// <summary>
    /// Create, update, delete and run tests. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageTestsAI = nameof(ManageTestsAI);

    /// <summary>
    /// Create, update and delete agents. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManageAgentsAI = nameof(ManageAgentsAI);

    /// <summary>
    /// Create, update and delete prompts. Needs the AI section and the matching user group right.
    /// </summary>
    public const string ManagePromptsAI = nameof(ManagePromptsAI);

    /// <summary>Registers every AI policy.</summary>
    internal static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AIAuthorizationPolicies.SectionAccessAI, policy =>
        {
            policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.Requirements.Add(new AISectionRequirement());
        });

        // One policy per right: the AI section plus the user group permission
        foreach (var (policyName, verb) in PermissionPolicies)
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
                policy.Requirements.Add(new AISectionRequirement());
                policy.Requirements.Add(new AIPermissionRequirement(verb));
            });
        }
    }

    private static readonly (string PolicyName, string Verb)[] PermissionPolicies =
    [
        (AIAuthorizationPolicies.ManageConnectionsAI, AIPermissions.ManageConnections),
        (AIAuthorizationPolicies.ManageProfilesAI, AIPermissions.ManageProfiles),
        (AIAuthorizationPolicies.ManageContextsAI, AIPermissions.ManageContexts),
        (AIAuthorizationPolicies.ManageKnowledgeSetsAI, AIPermissions.ManageKnowledgeSets),
        (AIAuthorizationPolicies.ManageGuardrailsAI, AIPermissions.ManageGuardrails),
        (AIAuthorizationPolicies.ManageSettingsAI, AIPermissions.ManageSettings),
        (AIAuthorizationPolicies.ViewAuditLogsAI, AIPermissions.ViewAuditLogs),
        (AIAuthorizationPolicies.ViewAnalyticsAI, AIPermissions.ViewAnalytics),
        (AIAuthorizationPolicies.ManageTestsAI, AIPermissions.ManageTests),
        (AIAuthorizationPolicies.ManageAgentsAI, AIPermissions.ManageAgents),
        (AIAuthorizationPolicies.ManagePromptsAI, AIPermissions.ManagePrompts),
    ];
}

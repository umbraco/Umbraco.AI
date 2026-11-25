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

    /// <summary>Registers every AI policy.</summary>
    internal static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(AIAuthorizationPolicies.SectionAccessAI, policy =>
        {
            policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.Requirements.Add(new AISectionRequirement());
        });

        // One policy per core right
        foreach (var (policyName, verb) in PermissionPolicies)
        {
            AddPermissionPolicy(options, policyName, verb);
        }
    }

    /// <summary>
    /// Adds a policy that needs the AI section and a user group right. An add-on calls this from its own
    /// authorization setup to guard its endpoints with a right it declares itself, with no change to core.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    /// <param name="policyName">The name of the policy, used in <c>[Authorize(Policy = ...)]</c>.</param>
    /// <param name="verb">The permission verb of the right, as ticked on the CMS user group screen.</param>
    public static void AddPermissionPolicy(AuthorizationOptions options, string policyName, string verb)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(verb);

        options.AddPolicy(policyName, policy =>
        {
            policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.Requirements.Add(new AISectionRequirement());
            policy.Requirements.Add(new AIPermissionRequirement(verb));
        });
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
    ];
}

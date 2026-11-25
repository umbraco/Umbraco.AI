namespace Umbraco.AI.Agent.Core.Security;

/// <summary>
/// The verbs of the agent rights ticked on the CMS user group screen. Each is stored as a user group
/// permission, so the values must never change.
/// </summary>
public static class AIAgentPermissions
{
    /// <summary>Create, update and delete agents.</summary>
    public const string Manage = "Uai.Agent.Manage";
}

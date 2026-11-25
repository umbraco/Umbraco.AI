namespace Umbraco.AI.Prompt.Core.Security;

/// <summary>
/// The verbs of the prompt rights ticked on the CMS user group screen. Each is stored as a user group
/// permission, so the values must never change.
/// </summary>
public static class AIPromptPermissions
{
    /// <summary>Create, update and delete prompts.</summary>
    public const string Manage = "Uai.Prompt.Manage";
}

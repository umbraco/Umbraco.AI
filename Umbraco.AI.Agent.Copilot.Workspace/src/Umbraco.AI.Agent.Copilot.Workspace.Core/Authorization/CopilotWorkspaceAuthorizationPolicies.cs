namespace Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;

/// <summary>
/// Authorization policy names for the Copilot Workspace. Lives in Core (as a plain string constant) so
/// both the Workspace stream/file controllers and the Conversations CRUD controllers can reference it —
/// closing the interrogation-1 gap where section access gated only the UI, not the stored corpus (F-SEC).
/// </summary>
public static class CopilotWorkspaceAuthorizationPolicies
{
    /// <summary>
    /// Requires the acting user to have access to the Copilot Workspace section.
    /// </summary>
    public const string SectionAccessCopilotWorkspace = nameof(SectionAccessCopilotWorkspace);
}

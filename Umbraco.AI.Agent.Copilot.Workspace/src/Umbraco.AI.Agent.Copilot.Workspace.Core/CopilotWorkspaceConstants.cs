namespace Umbraco.AI.Agent.Copilot.Workspace.Core;

/// <summary>
/// Shared constants for the Copilot Workspace product.
/// </summary>
public static class CopilotWorkspaceConstants
{
    /// <summary>
    /// Backoffice section aliases.
    /// </summary>
    public static class Sections
    {
        /// <summary>
        /// The Copilot Workspace backoffice section alias. This is a distinct section from the core AI
        /// admin section ("ai") so end-users can be granted the Workspace without AI configuration access.
        /// </summary>
        public const string CopilotWorkspace = "aiCopilotWorkspace";
    }
}

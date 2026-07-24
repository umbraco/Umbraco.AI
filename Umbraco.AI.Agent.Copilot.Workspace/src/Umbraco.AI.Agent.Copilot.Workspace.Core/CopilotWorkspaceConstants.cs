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
        public const string CopilotWorkspace = "Uai.Section.CopilotWorkspace";
    }

    /// <summary>
    /// Management API constants. One OpenAPI document per product (house convention): the Workspace
    /// stream/file controllers and the (reusable, host-agnostic) Conversations/Projects CRUD controllers
    /// all bind to this single document, separated into sub-areas by <c>[ApiExplorerSettings]</c> group
    /// names. The Conversations/Projects route segments and group names are owned by the Conversations web
    /// assembly (<c>ConversationsManagementApiConstants</c>); the stream endpoint deliberately shares the
    /// conversations route segment.
    /// </summary>
    public static class ManagementApi
    {
        /// <summary>The OpenAPI document name (matches the <c>[MapToApi]</c> value on controllers).</summary>
        public const string ApiName = "ai-copilot-workspace-management";

        /// <summary>The OpenAPI document title.</summary>
        public const string ApiTitle = "Umbraco AI Copilot Workspace Management API";

        /// <summary>Persisted stream + file endpoints.</summary>
        public static class Stream
        {
            /// <summary>OpenAPI group name for stream/file endpoints.</summary>
            public const string GroupName = "Stream";
        }
    }
}

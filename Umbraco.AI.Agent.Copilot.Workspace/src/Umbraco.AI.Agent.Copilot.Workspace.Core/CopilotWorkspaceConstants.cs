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
    /// Management API constants. One OpenAPI document per product (house convention): the Conversations
    /// CRUD controllers and the Workspace stream/file controllers all bind to this single document via
    /// <c>[MapToApi]</c>, separated into sub-areas by <c>[ApiExplorerSettings]</c> group names.
    /// </summary>
    public static class ManagementApi
    {
        /// <summary>The OpenAPI document name (matches the <c>[MapToApi]</c> value on controllers).</summary>
        public const string ApiName = "ai-copilot-workspace-management";

        /// <summary>The OpenAPI document title.</summary>
        public const string ApiTitle = "Umbraco AI Copilot Workspace Management API";

        /// <summary>Conversation CRUD endpoints.</summary>
        public static class Conversations
        {
            /// <summary>Route segment for conversation endpoints.</summary>
            public const string RouteSegment = "conversations";

            /// <summary>OpenAPI group name for conversation endpoints.</summary>
            public const string GroupName = "Conversations";
        }

        /// <summary>Project CRUD endpoints.</summary>
        public static class Projects
        {
            /// <summary>Route segment for project endpoints.</summary>
            public const string RouteSegment = "projects";

            /// <summary>OpenAPI group name for project endpoints.</summary>
            public const string GroupName = "Projects";
        }

        /// <summary>Persisted stream + file endpoints.</summary>
        public static class Stream
        {
            /// <summary>OpenAPI group name for stream/file endpoints.</summary>
            public const string GroupName = "Stream";
        }
    }
}

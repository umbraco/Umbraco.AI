namespace Umbraco.AI.Agent.Core;

/// <summary>
/// Constants for Umbraco AI Agent.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Keys for RuntimeContext.
    /// </summary>
    public static class ContextKeys
    {
        /// <summary>
        /// Key for frontend tool names in RuntimeContext.
        /// Used by <see cref="Chat.AIToolReorderingChatClient"/> to identify which tools
        /// are frontend tools that should be processed last.
        /// </summary>
        public const string FrontendToolNames = "Umbraco.AI.Agent.FrontendToolNames";

        /// <summary>
        /// Key for agent ID in metadata collections.
        /// </summary>
        public const string AgentId = "Umbraco.AI.Agent.AgentId";

        /// <summary>
        /// Key for agent alias in metadata collections.
        /// </summary>
        public const string AgentAlias = "Umbraco.AI.Agent.AgentAlias";

        /// <summary>
        /// Key for run ID in metadata collections.
        /// </summary>
        public const string RunId = "Umbraco.AI.Agent.RunId";

        /// <summary>
        /// Key for thread ID in metadata collections.
        /// </summary>
        public const string ThreadId = "Umbraco.AI.Agent.ThreadId";
    }

    /// <summary>
    /// Dimensions that can be used to scope agents to specific contexts, such as sections of the Umbraco backoffice or entity types. These are used by surfaces to determine which agents to show for a given context.
    /// </summary>
    public class AgentScopeDimensions
    {
        /// <summary>
        /// Dimension for scoping agents to specific sections of the Umbraco backoffice. For example, an agent with a scope of "section:content" would only be shown when the user is in the Content section of the backoffice.
        /// </summary>
        public const string Section = "section";

        /// <summary>
        /// Dimension for scoping agents to specific entity types. For example, an agent with a scope of "entityType:document" would only be shown when the user is editing a document in the backoffice.
        /// </summary>
        public const string EntityType = "entity-type";
    }
}

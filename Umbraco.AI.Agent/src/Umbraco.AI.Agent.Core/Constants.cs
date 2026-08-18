namespace Umbraco.AI.Agent.Core;

/// <summary>
/// Constants for Umbraco AI Agent.
/// </summary>
public static class Constants
{
    /// <summary>
    /// File system paths used by the agent.
    /// </summary>
    public static class SystemDirectories
    {
        /// <summary>
        /// Content-root-relative directory holding conversation file uploads.
        /// </summary>
        /// <remarks>
        /// Under the content root rather than the web root, so it is NOT reachable over HTTP. These are
        /// private user uploads and are only ever served through the authenticated, owner-scoped file
        /// endpoint. Anything served statically (the media directory in particular) is the wrong home
        /// for them.
        /// </remarks>
        public const string ConversationFiles = Umbraco.Cms.Core.Constants.SystemDirectories.TempData + "/UmbracoAIAgent";

        /// <summary>
        /// Directory, relative to the media file system, that earlier versions wrote conversation
        /// uploads into. Retained so the upgrade path can delete what was left there; nothing writes
        /// to it any more.
        /// </summary>
        public const string LegacyPublicConversationFiles = "agui-files";
    }

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
        /// Key for the runtime-context system prompt an agent run wants placed at the head of the
        /// conversation. Set by <see cref="Chat.ScopedAIAgent"/> and consumed by
        /// <see cref="Chat.AIAgentSystemMessageChatClient"/>, which is the first point that sees the
        /// stored history and the new turn as one list.
        /// </summary>
        public const string PendingSystemMessage = "Umbraco.AI.Agent.PendingSystemMessage";

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

        /// <summary>
        /// Key for surface ID in runtime context.
        /// Identifies which UI surface the request originated from (e.g., "copilot", "workspace").
        /// </summary>
        public const string Surface = "Umbraco.AI.Agent.Surface";

        /// <summary>
        /// Key for the persisted conversation ID in runtime context. Populated by surfaces with
        /// server-side conversation persistence (Copilot Workspace); absent otherwise. Lets telemetry
        /// and notifications correlate a run with its durable conversation.
        /// </summary>
        public const string ConversationId = "Umbraco.AI.Agent.ConversationId";

        /// <summary>
        /// Key for the caller's already-resolved allowed tool IDs.
        /// Set by <see cref="Agents.AIAgentService"/> so the agent factory builds its server-side
        /// tool list from the same permission decision that filtered the frontend tools — including
        /// any per-user-group allows and denies. Without it the factory would fall back to the
        /// agent's own defaults and silently ignore user group permissions.
        /// </summary>
        public const string AllowedToolIds = "Umbraco.AI.Agent.AllowedToolIds";
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

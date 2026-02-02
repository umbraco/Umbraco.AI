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
}
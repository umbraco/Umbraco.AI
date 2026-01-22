namespace Umbraco.Ai.Agent.Core;

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
        /// Used by <see cref="Chat.AiToolReorderingChatClient"/> to identify which tools
        /// are frontend tools that should be processed last.
        /// </summary>
        public const string FrontendToolNames = "Umbraco.Ai.Agent.FrontendToolNames";
        
        /// <summary>
        /// Key for agent ID in metadata collections.
        /// </summary>
        public const string AgentId = "Umbraco.Ai.Agent.AgentId";
        
        /// <summary>
        /// Key for agent alias in metadata collections.
        /// </summary>
        public const string AgentAlias = "Umbraco.Ai.Agent.AgentAlias";
        
        /// <summary>
        /// Key for run ID in metadata collections.
        /// </summary>
        public const string RunId = "Umbraco.Ai.Agent.RunId";
        
        /// <summary>
        /// Key for thread ID in metadata collections.
        /// </summary>
        public const string ThreadId = "Umbraco.Ai.Agent.ThreadId";
    }
}
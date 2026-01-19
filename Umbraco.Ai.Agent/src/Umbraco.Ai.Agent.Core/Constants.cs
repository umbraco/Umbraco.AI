namespace Umbraco.Ai.Agent.Core;

/// <summary>
/// Constants for Umbraco AI Agent.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Keys for metadata in metadata collections.
    /// </summary>
    public static class MetadataKeys
    {
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

    /// <summary>
    /// Keys for ChatOptions.AdditionalProperties.
    /// </summary>
    public static class ChatOptionsKeys
    {
        /// <summary>
        /// Key for frontend tool names in ChatOptions.AdditionalProperties.
        /// Used by <see cref="Chat.AiToolReorderingChatClient"/> to identify which tools
        /// are frontend tools that should be processed last.
        /// </summary>
        public const string FrontendToolNames = "Umbraco.Ai.Agent.FrontendToolNames";
    }
}
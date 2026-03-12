namespace Umbraco.AI.Prompt.Core;

/// <summary>
/// Constants for Umbraco AI Prompt.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Keys for metadata in metadata collections.
    /// </summary>
    public static class MetadataKeys
    {
        /// <summary>
        /// Key for prompt ID in metadata collections.
        /// </summary>
        public const string PromptId = "Umbraco.AI.PromptId";
        
        /// <summary>
        /// Key for prompt alias in metadata collections.
        /// </summary>
        public const string PromptAlias = "Umbraco.AI.PromptAlias";

        /// <summary>
        /// Key for context IDs override in runtime context.
        /// When set, context resolvers use these IDs instead of the prompt's configured <see cref="Prompts.AIPrompt.ContextIds"/>.
        /// </summary>
        public const string ContextIdsOverride = "Umbraco.AI.Prompt.ContextIdsOverride";
    }
} 
using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.AI.Core.Serialization;

namespace Umbraco.AI.Core;

/// <summary>
/// Constants for Umbraco.AI.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Default JSON serializer options for Umbraco.AI.
    /// </summary>
    public static JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonStringTypeConverter()
        }
    };

    /// <summary>
    /// Section constants for Umbraco.AI. These can be used for checking the current section in Umbraco backoffice or for storing section information in metadata, etc.
    /// </summary>
    internal static class Sections
    {
        /// <summary>
        /// Section alias for Umbraco.AI.
        /// </summary>
        public const string AI = "ai";
    }

    /// <summary>
    /// Property editor constants for Umbraco.AI.
    /// </summary>
    internal static class PropertyEditors
    {
        /// <summary>
        /// Property editor aliases.
        /// </summary>
        internal static class Aliases
        {
            /// <summary>
            /// Alias for the AI Context Picker property editor.
            /// </summary>
            public const string ContextPicker = "Uai.ContextPicker";
        }
    }

    /// <summary>
    /// Well-known feature type values used in runtime context for telemetry, audit, and analytics.
    /// </summary>
    public static class FeatureTypes
    {
        /// <summary>
        /// Feature type for inline chat executions via <see cref="Chat.IAIChatService.GetChatResponseAsync"/>.
        /// </summary>
        public const string InlineChat = "inline-chat";

        /// <summary>
        /// Feature type for inline agent executions.
        /// </summary>
        public const string InlineAgent = "inline-agent";

        /// <summary>
        /// Feature type for inline speech-to-text executions via <see cref="SpeechToText.IAISpeechToTextService.TranscribeAsync"/>.
        /// </summary>
        public const string InlineSpeechToText = "inline-speech-to-text";

        /// <summary>
        /// Feature type for inline embedding executions via <see cref="Embeddings.IAIEmbeddingService.GenerateEmbeddingsAsync"/>.
        /// </summary>
        public const string InlineEmbedding = "inline-embedding";
    }

    /// <summary>
    /// Keys for metadata in metadata collections.
    /// </summary>
    public static class ContextKeys
    {
        /// <summary>
        /// Key for profile ID in metadata collections.
        /// </summary>
        public const string ProfileId = "Umbraco.AI.ProfileId";

        /// <summary>
        /// Key for profile alias in metadata collections.
        /// </summary>
        public const string ProfileAlias = "Umbraco.AI.ProfileAlias";

        /// <summary>
        /// Key for provider ID in metadata collections.
        /// </summary>
        public const string ProviderId = "Umbraco.AI.ProviderId";

        /// <summary>
        /// Key for model ID in metadata collections.
        /// </summary>
        public const string ModelId = "Umbraco.AI.ModelId";

        /// <summary>
        /// Key for feature ID in metadata collections.
        /// </summary>
        public const string FeatureId = "Umbraco.AI.FeatureId";

        /// <summary>
        /// Key for feature alias in metadata collections.
        /// </summary>
        public const string FeatureAlias = "Umbraco.AI.FeatureAlias";

        /// <summary>
        /// Key for feature type in metadata collections.
        /// </summary>
        public const string FeatureType = "Umbraco.AI.FeatureType";

        /// <summary>
        /// Key for profile version in metadata collections.
        /// </summary>
        public const string ProfileVersion = "Umbraco.AI.ProfileVersion";

        /// <summary>
        /// Key for feature version in metadata collections.
        /// </summary>
        public const string FeatureVersion = "Umbraco.AI.FeatureVersion";

        /// <summary>
        /// Key for entity ID in metadata collections.
        /// </summary>
        public const string EntityId = "Umbraco.AI.EntityId";

        /// <summary>
        /// Key for entity type in metadata collections.
        /// </summary>
        public const string EntityType = "Umbraco.AI.EntityType";

        /// <summary>
        /// Key for section pathname in metadata collections.
        /// </summary>
        /// <example>"content", "media", "settings"</example>
        public const string Section = "Umbraco.AI.Section";

        /// <summary>
        /// Key for log keys in metadata collections.
        /// </summary>
        public const string LogKeys = "Umbraco.AI.LogKeys";

        /// <summary>
        /// Key for <see cref="EntityAdapter.AISerializedEntity"/> data.
        /// </summary>
        public const string SerializedEntity = "Umbraco.AI.SerializedEntity";

        /// <summary>
        /// Key for the parent entity unique identifier (as Guid).
        /// Set when creating a new entity under a parent.
        /// </summary>
        public const string ParentEntityId = "Umbraco.AI.ParentEntityId";

        /// <summary>
        /// Key indicating that the current execution is a guardrail evaluation call.
        /// Used to prevent infinite recursion when the LLM guardrail evaluator calls the chat service.
        /// </summary>
        public const string IsGuardrailEvaluation = "Umbraco.AI.IsGuardrailEvaluation";

        /// <summary>
        /// Key for guardrail IDs override in runtime context.
        /// When set, the guardrail resolution system uses these IDs instead of (or in addition to)
        /// the guardrails configured on the profile, prompt, or agent.
        /// Used by the test execution system to override guardrails for testing scenarios.
        /// </summary>
        public const string GuardrailIdsOverride = "Umbraco.AI.GuardrailIdsOverride";

        /// <summary>
        /// Key for chat options override in runtime context.
        /// When set, the <see cref="Chat.AIChatOptionsOverrideChatMiddleware"/> merges these options
        /// into the call's options (override values take precedence).
        /// Used by inline agent and inline chat builders to pass ChatOptions through the middleware pipeline.
        /// </summary>
        public const string ChatOptionsOverride = "Umbraco.AI.ChatOptionsOverride";
    }
}

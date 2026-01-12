using System.Diagnostics;

namespace Umbraco.Ai.Core.Governance;

/// <summary>
/// Provides OpenTelemetry ActivitySource and constants for AI telemetry.
/// </summary>
public static class AiTelemetrySource
{
    /// <summary>
    /// The name of the ActivitySource for Umbraco AI operations.
    /// </summary>
    public const string ActivitySourceName = "Umbraco.Ai";

    /// <summary>
    /// The ActivitySource for creating Activities in Umbraco AI operations.
    /// </summary>
    public static readonly ActivitySource Source = new(ActivitySourceName, "1.0.0");

    // Activity names
    /// <summary>
    /// Activity name for chat request operations.
    /// </summary>
    public const string ChatRequestActivity = "ai.chat.request";

    /// <summary>
    /// Activity name for embedding request operations.
    /// </summary>
    public const string EmbeddingRequestActivity = "ai.embedding.request";

    /// <summary>
    /// Activity name for profile resolution operations.
    /// </summary>
    public const string ProfileResolutionActivity = "ai.profile.resolution";

    /// <summary>
    /// Activity name for context resolution operations.
    /// </summary>
    public const string ContextResolutionActivity = "ai.context.resolution";

    // Tag names (following OpenTelemetry semantic conventions)
    /// <summary>
    /// Tag for AI profile ID.
    /// </summary>
    public const string ProfileIdTag = "ai.profile.id";

    /// <summary>
    /// Tag for AI profile alias.
    /// </summary>
    public const string ProfileAliasTag = "ai.profile.alias";

    /// <summary>
    /// Tag for AI provider ID.
    /// </summary>
    public const string ProviderIdTag = "ai.provider.id";

    /// <summary>
    /// Tag for AI model ID.
    /// </summary>
    public const string ModelIdTag = "ai.model.id";

    /// <summary>
    /// Tag for feature type (e.g., "prompt", "agent").
    /// </summary>
    public const string FeatureTypeTag = "ai.feature.type";

    /// <summary>
    /// Tag for feature ID (prompt or agent ID).
    /// </summary>
    public const string FeatureIdTag = "ai.feature.id";

    /// <summary>
    /// Tag for user ID.
    /// </summary>
    public const string UserIdTag = "ai.user.id";

    /// <summary>
    /// Tag for input token count.
    /// </summary>
    public const string TokensInputTag = "ai.tokens.input";

    /// <summary>
    /// Tag for output token count.
    /// </summary>
    public const string TokensOutputTag = "ai.tokens.output";

    /// <summary>
    /// Tag for total token count.
    /// </summary>
    public const string TokensTotalTag = "ai.tokens.total";

    /// <summary>
    /// Tag for entity ID.
    /// </summary>
    public const string EntityIdTag = "ai.entity.id";

    /// <summary>
    /// Tag for entity type.
    /// </summary>
    public const string EntityTypeTag = "ai.entity.type";

    /// <summary>
    /// Tag for operation type.
    /// </summary>
    public const string OperationTypeTag = "ai.operation.type";
}

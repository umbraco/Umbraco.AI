using System.Diagnostics;

namespace Umbraco.Ai.Core.Audit;

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
    /// Tag for AI model ID.
    /// </summary>
    public const string ModelIdTag = "ai.model.id";

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
    /// Tag for operation type.
    /// </summary>
    public const string OperationTypeTag = "ai.operation.type";
}

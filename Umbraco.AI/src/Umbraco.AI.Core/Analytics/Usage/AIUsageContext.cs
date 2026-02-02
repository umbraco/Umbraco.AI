using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Contains metadata for an AI usage operation.
/// Extracted from ChatOptions/EmbeddingGenerationOptions AdditionalProperties.
/// </summary>
public sealed class AiUsageContext
{
    /// <summary>
    /// Gets the AI capability being executed (Chat, Embedding, etc.).
    /// </summary>
    public required AiCapability Capability { get; init; }

    /// <summary>
    /// Gets the profile ID used for this operation.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// Gets the profile alias.
    /// </summary>
    public string? ProfileAlias { get; init; }

    /// <summary>
    /// Gets the provider ID (e.g., "openai", "azure").
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the model ID used for this operation.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Gets the entity ID this operation is associated with (e.g., content item ID).
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., "content", "media").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets the feature type that initiated this operation (e.g., "prompt", "agent").
    /// </summary>
    public string? FeatureType { get; init; }

    /// <summary>
    /// Gets the feature ID (prompt or agent ID) that initiated this operation.
    /// </summary>
    public Guid? FeatureId { get; init; }

    /// <summary>
    /// Extracts usage context from runtime context.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="runtimeContext">The runtime context containing additional properties.</param>
    /// <param name="modelId">Optional model ID to override runtime context value.</param>
    /// <returns>An AiUsageContext populated with available metadata.</returns>
    public static AiUsageContext ExtractFromRuntimeContext(
        AiCapability capability,
        AiRuntimeContext runtimeContext,
        string? modelId = null)
    {
        return new AiUsageContext
        {
            Capability = capability,
            ProfileId = runtimeContext.GetValue<Guid>(Constants.ContextKeys.ProfileId),
            ProfileAlias = runtimeContext.GetValue<string>(Constants.ContextKeys.ProfileAlias),
            ProviderId = runtimeContext.GetValue<string>(Constants.ContextKeys.ProviderId),
            ModelId = modelId ?? runtimeContext.GetValue<string>(Constants.ContextKeys.ModelId),
            EntityId = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityId),
            EntityType = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityType),
            FeatureType = runtimeContext.GetValue<string>(Constants.ContextKeys.FeatureType),
            FeatureId = runtimeContext.GetValue<Guid>(Constants.ContextKeys.FeatureId)
        };
    }
}

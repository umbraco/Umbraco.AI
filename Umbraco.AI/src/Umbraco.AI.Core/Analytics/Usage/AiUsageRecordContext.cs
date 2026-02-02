using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Analytics.Usage;

/// <summary>
/// Context information for creating an AI usage record.
/// Contains all metadata needed by the factory to create a complete record.
/// </summary>
public sealed class AiUsageRecordContext
{
    /// <summary>
    /// Gets the AI capability being executed (Chat, Embedding, etc.).
    /// </summary>
    public required AiCapability Capability { get; init; }

    /// <summary>
    /// Gets the profile ID used for this operation.
    /// </summary>
    public required Guid ProfileId { get; init; }

    /// <summary>
    /// Gets the profile alias.
    /// </summary>
    public required string ProfileAlias { get; init; }

    /// <summary>
    /// Gets the provider ID (e.g., "openai", "azure").
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the model ID used for this operation.
    /// </summary>
    public required string ModelId { get; init; }

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
    /// Creates a context from AiUsageContext (for middleware compatibility).
    /// </summary>
    /// <param name="usageContext">The AiUsageContext extracted from options.</param>
    /// <returns>A new AiUsageRecordContext with validated required fields.</returns>
    public static AiUsageRecordContext FromUsageContext(AiUsageContext usageContext)
    {
        return new AiUsageRecordContext
        {
            Capability = usageContext.Capability,
            ProfileId = usageContext.ProfileId ?? Guid.Empty,
            ProfileAlias = usageContext.ProfileAlias ?? "unknown",
            ProviderId = usageContext.ProviderId ?? "unknown",
            ModelId = usageContext.ModelId ?? "unknown",
            EntityId = usageContext.EntityId,
            EntityType = usageContext.EntityType,
            FeatureType = usageContext.FeatureType,
            FeatureId = usageContext.FeatureId
        };
    }
}

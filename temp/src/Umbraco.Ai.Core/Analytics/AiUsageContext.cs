using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Analytics;

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
    /// Extracts usage context from ChatOptions.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="options">The ChatOptions containing additional properties.</param>
    /// <returns>An AiUsageContext populated with available metadata.</returns>
    public static AiUsageContext ExtractFromOptions(
        AiCapability capability,
        ChatOptions? options)
        => ExtractFromAdditionalProperties(
            capability,
            options?.ModelId,
            options?.AdditionalProperties);

    /// <summary>
    /// Extracts usage context from EmbeddingGenerationOptions.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="options">The EmbeddingGenerationOptions containing additional properties.</param>
    /// <returns>An AiUsageContext populated with available metadata.</returns>
    public static AiUsageContext ExtractFromOptions(
        AiCapability capability,
        EmbeddingGenerationOptions? options)
        => ExtractFromAdditionalProperties(
            capability,
            options?.ModelId,
            options?.AdditionalProperties);

    private static AiUsageContext ExtractFromAdditionalProperties(
        AiCapability capability,
        string? modelId,
        AdditionalPropertiesDictionary? additionalProperties)
    {
        return new AiUsageContext
        {
            Capability = capability,
            ProfileId = GetGuid(additionalProperties, Constants.MetadataKeys.ProfileId),
            ProfileAlias = GetString(additionalProperties, Constants.MetadataKeys.ProfileAlias),
            ProviderId = GetString(additionalProperties, Constants.MetadataKeys.ProviderId),
            ModelId = modelId ?? GetString(additionalProperties, Constants.MetadataKeys.ModelId),
            EntityId = GetString(additionalProperties, Constants.MetadataKeys.EntityId),
            EntityType = GetString(additionalProperties, Constants.MetadataKeys.EntityType),
            FeatureType = GetString(additionalProperties, Constants.MetadataKeys.FeatureType),
            FeatureId = GetNullableGuid(additionalProperties, Constants.MetadataKeys.FeatureId)
        };
    }

    private static Guid? GetGuid(AdditionalPropertiesDictionary? props, string key)
    {
        if (props?.TryGetValue(key, out var value) == true)
        {
            if (value is Guid guid) return guid;
            if (value is string str && Guid.TryParse(str, out guid)) return guid;
        }
        return null;
    }

    private static Guid? GetNullableGuid(AdditionalPropertiesDictionary? props, string key)
        => GetGuid(props, key);

    private static string? GetString(AdditionalPropertiesDictionary? props, string key)
        => props?.TryGetValue(key, out var value) == true ? value?.ToString() : null;
}

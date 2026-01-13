using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Audit;

/// <summary>
/// Contains all metadata for an AI audit operation.
/// Completely independent of OpenTelemetry Activity.
/// </summary>
public sealed class AiAuditContext
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
    /// Gets the prompt or input data for this operation.
    /// </summary>
    public object? Prompt { get; init; }

    /// <summary>
    /// Extracts audit context from ChatOptions and current user.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="options">The ChatOptions containing additional properties.</param>
    /// <param name="prompt">The prompt or input data.</param>
    /// <returns>An AiAuditContext populated with available metadata.</returns>
    public static AiAuditContext ExtractFromOptions(
        AiCapability capability,
        ChatOptions? options,
        object? prompt)
    {
        var props = options?.AdditionalProperties;

        return new AiAuditContext
        {
            Capability = capability,
            ProfileId = GetGuid(props, Constants.MetadataKeys.ProfileId),
            ProfileAlias = GetString(props, Constants.MetadataKeys.ProfileAlias),
            ProviderId = GetString(props, Constants.MetadataKeys.ProviderId),
            ModelId = options?.ModelId,
            EntityId = GetString(props, Constants.MetadataKeys.EntityId),
            EntityType = GetString(props, Constants.MetadataKeys.EntityType),
            FeatureType = GetString(props, Constants.MetadataKeys.FeatureType),
            FeatureId = GetNullableGuid(props, Constants.MetadataKeys.FeatureId),
            Prompt = prompt
        };
    }

    private static Guid GetGuid(AdditionalPropertiesDictionary? props, string key)
    {
        if (props?.TryGetValue(key, out var value) == true)
        {
            if (value is Guid guid) return guid;
            if (value is string str && Guid.TryParse(str, out guid)) return guid;
        }
        return Guid.Empty;
    }

    private static Guid? GetNullableGuid(AdditionalPropertiesDictionary? props, string key)
    {
        var guid = GetGuid(props, key);
        return guid == Guid.Empty ? null : guid;
    }

    private static string? GetString(AdditionalPropertiesDictionary? props, string key)
        => props?.TryGetValue(key, out var value) == true ? value?.ToString() : null;
}

using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.AuditLog;

/// <summary>
/// Contains all metadata for an AI audit-log operation.
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
    /// Gets the prompt or input data for this operation.
    /// </summary>
    public object? Prompt { get; init; }

    /// <summary>
    /// Gets extensible metadata for feature-specific context (e.g., AgentRunId, ThreadId, ConversationId).
    /// </summary>
    public Dictionary<string, string>? Metadata { get; } = new();

    /// <summary>
    /// Extracts audit-log context from ChatOptions and current user.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="options">The ChatOptions containing additional properties.</param>
    /// <param name="prompt">The prompt or input data.</param>
    /// <returns>An AiAuditLogContext populated with available metadata.</returns>
    public static AiAuditContext ExtractFromOptions(
        AiCapability capability,
        ChatOptions? options,
        object? prompt)
        => ExtractFromAdditionalProperties(
            capability,
            options?.ModelId,
            options?.AdditionalProperties,
            prompt);

    /// <summary>
    /// Extracts audit-log context from EmbeddingGenerationOptions.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="options">The EmbeddingGenerationOptions containing additional properties.</param>
    /// <param name="prompt">The prompt or input data.</param>
    /// <returns>An AiAuditLogContext populated with available metadata.</returns>
    public static AiAuditContext ExtractFromOptions(
        AiCapability capability,
        EmbeddingGenerationOptions? options,
        object? prompt)
        => ExtractFromAdditionalProperties(
            capability,
            options?.ModelId,
            options?.AdditionalProperties,
            prompt);
    
    private static AiAuditContext ExtractFromAdditionalProperties(
        AiCapability capability,
        string? modelId,
        AdditionalPropertiesDictionary? additionalProperties,
        object? prompt)
    {
        return new AiAuditContext
        {
            Capability = capability,
            ProfileId = GetGuid(additionalProperties, Constants.MetadataKeys.ProfileId),
            ProfileAlias = GetString(additionalProperties, Constants.MetadataKeys.ProfileAlias),
            ProviderId = GetString(additionalProperties, Constants.MetadataKeys.ProviderId),
            ModelId = modelId,
            EntityId = GetString(additionalProperties, Constants.MetadataKeys.EntityId),
            EntityType = GetString(additionalProperties, Constants.MetadataKeys.EntityType),
            FeatureType = GetString(additionalProperties, Constants.MetadataKeys.FeatureType),
            FeatureId = GetNullableGuid(additionalProperties, Constants.MetadataKeys.FeatureId),
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

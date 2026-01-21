using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.RuntimeContext;

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
    /// <param name="runtimeContext">The runtime context containing additional properties.</param>
    /// <param name="prompt">The prompt or input data.</param>
    /// <param name="modelId">Optional model ID to override runtime context value.</param>
    /// <returns>An AiAuditLogContext populated with available metadata.</returns>
    public static AiAuditContext ExtractFromRuntimeContext(
        AiCapability capability,
        AiRuntimeContext runtimeContext,
        object? prompt,
        string? modelId = null)
    {
        return new AiAuditContext
        {
            Capability = capability,
            ProfileId = runtimeContext.GetValue<Guid>(Constants.MetadataKeys.ProfileId),
            ProfileAlias = runtimeContext.GetValue<string>(Constants.MetadataKeys.ProfileAlias),
            ProviderId = runtimeContext.GetValue<string>(Constants.MetadataKeys.ProviderId),
            ModelId = modelId ?? runtimeContext.GetValue<string>(Constants.MetadataKeys.ModelId),
            EntityId = runtimeContext.GetValue<string>(Constants.MetadataKeys.EntityId),
            EntityType = runtimeContext.GetValue<string>(Constants.MetadataKeys.EntityType),
            FeatureType = runtimeContext.GetValue<string>(Constants.MetadataKeys.FeatureType),
            FeatureId = runtimeContext.GetValue<Guid>(Constants.MetadataKeys.FeatureId),
            Prompt = prompt
        };
    }
}

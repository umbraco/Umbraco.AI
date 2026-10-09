using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.AuditLog;

/// <summary>
/// Contains all metadata for an AI audit-log operation.
/// Completely independent of OpenTelemetry Activity.
/// </summary>
public sealed class AIAuditContext
{

    /// <summary>
    /// Gets the AI capability being executed (Chat, Embedding, etc.).
    /// </summary>
    public required AICapability Capability { get; init; }

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
    /// Gets the profile version at time of execution.
    /// </summary>
    public int? ProfileVersion { get; init; }

    /// <summary>
    /// Gets the feature version at time of execution.
    /// </summary>
    public int? FeatureVersion { get; init; }

    /// <summary>
    /// Gets the prompt or input data for this operation.
    /// </summary>
    public object? Prompt { get; init; }

    /// <summary>
    /// Gets extensible metadata for feature-specific context (e.g., AgentRunId, ThreadId, ConversationId).
    /// </summary>
    /// <remarks>
    /// Never populated or read: the audit entry's metadata comes from the declared log keys, passed to
    /// <see cref="IAIAuditLogFactory"/> separately.
    /// </remarks>
    [Obsolete("Never populated; audit metadata comes from the declared log keys. Will be removed in v20.")]
    public Dictionary<string, string>? Metadata { get; } = new();

    /// <summary>
    /// Extracts audit-log context from ChatOptions and current user.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="runtimeContext">The runtime context containing additional properties.</param>
    /// <param name="prompt">The prompt or input data.</param>
    /// <param name="modelId">Optional model ID to override runtime context value.</param>
    /// <returns>An AIAuditLogContext populated with available metadata.</returns>
    public static AIAuditContext ExtractFromRuntimeContext(
        AICapability capability,
        AIRuntimeContext runtimeContext,
        object? prompt,
        string? modelId = null)
    {
        return new AIAuditContext
        {
            Capability = capability,
            ProfileId = runtimeContext.GetValue<Guid>(Constants.ContextKeys.ProfileId),
            ProfileAlias = runtimeContext.GetValue<string>(Constants.ContextKeys.ProfileAlias),
            ProviderId = runtimeContext.GetValue<string>(Constants.ContextKeys.ProviderId),
            ModelId = modelId ?? runtimeContext.GetValue<string>(Constants.ContextKeys.ModelId),
            EntityId = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityId),
            EntityType = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityType),
            FeatureType = runtimeContext.GetValue<string>(Constants.ContextKeys.FeatureType),
            FeatureId = runtimeContext.GetValue<Guid>(Constants.ContextKeys.FeatureId),
            ProfileVersion = runtimeContext.GetValue<int>(Constants.ContextKeys.ProfileVersion),
            FeatureVersion = runtimeContext.GetValue<int>(Constants.ContextKeys.FeatureVersion),
            Prompt = prompt
        };
    }

    /// <summary>
    /// Builds audit-log context from a call's identity as the operation tracker captured it, so the audit
    /// log and the other recorders read the runtime context once, at the start of the call.
    /// </summary>
    internal static AIAuditContext FromUsageContext(AIUsageContext identity, object? prompt) => new()
    {
        Capability = identity.Capability,
        ProfileId = identity.ProfileId,
        ProfileAlias = identity.ProfileAlias,
        ProviderId = identity.ProviderId,
        ModelId = identity.ModelId,
        EntityId = identity.EntityId,
        EntityType = identity.EntityType,
        FeatureType = identity.FeatureType,
        FeatureId = identity.FeatureId,
        ProfileVersion = identity.ProfileVersion,
        FeatureVersion = identity.FeatureVersion,
        Prompt = prompt,
    };
}

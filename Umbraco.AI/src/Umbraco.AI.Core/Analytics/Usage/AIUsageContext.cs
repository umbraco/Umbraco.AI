using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Analytics.Usage;

/// <summary>
/// The identity of an AI call: capability, profile, provider, model, entity and feature, with versions.
/// Read from the runtime context once, when a tracked call starts, and shared by every recorder (usage
/// analytics, the audit log, trace tags, test-run usage).
/// </summary>
/// <remarks>
/// The name predates that wider use; it is kept because the type is public.
/// </remarks>
public sealed class AIUsageContext
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
    /// Gets the feature alias (prompt or agent alias, or a built-in feature such as "guardrail-llm-evaluator") that initiated this operation.
    /// </summary>
    public string? FeatureAlias { get; init; }

    /// <summary>
    /// Gets the profile version at time of execution.
    /// </summary>
    public int? ProfileVersion { get; init; }

    /// <summary>
    /// Gets the feature version at time of execution.
    /// </summary>
    public int? FeatureVersion { get; init; }

    /// <summary>
    /// Extracts usage context from runtime context.
    /// </summary>
    /// <param name="capability">The AI capability being used.</param>
    /// <param name="runtimeContext">The runtime context containing additional properties.</param>
    /// <param name="modelId">Optional model ID to override runtime context value.</param>
    /// <returns>An AIUsageContext populated with available metadata.</returns>
    public static AIUsageContext ExtractFromRuntimeContext(
        AICapability capability,
        AIRuntimeContext runtimeContext,
        string? modelId = null)
    {
        return new AIUsageContext
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
            FeatureAlias = runtimeContext.GetValue<string>(Constants.ContextKeys.FeatureAlias),
            ProfileVersion = runtimeContext.GetValue<int>(Constants.ContextKeys.ProfileVersion),
            FeatureVersion = runtimeContext.GetValue<int>(Constants.ContextKeys.FeatureVersion)
        };
    }
}

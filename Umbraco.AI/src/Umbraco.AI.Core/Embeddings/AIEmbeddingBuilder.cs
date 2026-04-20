using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Core.Embeddings;

/// <summary>
/// Fluent builder for configuring inline embedding executions — embedding generations that run purely in code
/// with full observability (notifications, telemetry, duration tracking).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// var embeddings = await embeddingService.GenerateEmbeddingsAsync(emb => emb
///     .WithAlias("content-search")
///     .WithProfile("ada-embedding")
///     .WithGuardrails(guardrailId),   // additive on top of the profile's guardrails
///     values, cancellationToken);
/// </code>
/// </remarks>
public sealed class AIEmbeddingBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5)
    // Different from inline chat and speech-to-text namespaces to avoid ID collisions
    private static readonly Guid InlineEmbeddingNamespace = new("D7E6F8A9-5B2C-4D3E-8F1A-6C9B0D2E4F7A");

    private string? _alias;
    private string? _name;
    private string? _description;
    private Guid? _profileId;
    private string? _profileAlias;
    private EmbeddingGenerationOptions? _embeddingOptions;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private readonly Guardrails.AIGuardrailBuilderState _aiGuardrails = new();
    private IReadOnlyDictionary<string, object?>? _additionalProperties;
    private bool _isPassThrough;

    /// <summary>
    /// Sets the alias for the inline embedding. Required for auditing and telemetry.
    /// </summary>
    /// <param name="alias">A unique, URL-safe identifier for this inline embedding.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithAlias(string alias)
    {
        _alias = alias;
        _id = null;
        return this;
    }

    /// <summary>
    /// Sets the display name for the inline embedding.
    /// If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description for the inline embedding.
    /// </summary>
    /// <param name="description">The description of what this embedding does.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by ID.
    /// If not set, the default embedding profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        _profileAlias = null;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by alias.
    /// If not set, the default embedding profile is used.
    /// </summary>
    /// <param name="profileAlias">The profile alias.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithProfile(string profileAlias)
    {
        _profileAlias = profileAlias;
        _profileId = null;
        return this;
    }

    /// <summary>
    /// Sets embedding generation options to override profile defaults (model, dimensions, etc.).
    /// </summary>
    /// <param name="options">The embedding generation options to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithEmbeddingOptions(EmbeddingGenerationOptions options)
    {
        _embeddingOptions = options;
        return this;
    }

    /// <summary>
    /// Sets context items to populate the runtime context with.
    /// </summary>
    /// <param name="contextItems">The context items.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Adds guardrails on top of the profile's configured guardrails (additive). Use
    /// <see cref="SetGuardrails(Guid[])"/> to fully replace.
    /// </summary>
    public AIEmbeddingBuilder WithGuardrails(params Guid[] guardrailIds)
    {
        _aiGuardrails.With(guardrailIds);
        return this;
    }

    /// <summary>
    /// Adds guardrails by alias on top of the profile's configured guardrails (additive). Aliases are
    /// resolved to IDs by the service layer.
    /// </summary>
    public AIEmbeddingBuilder WithGuardrails(params string[] guardrailAliases)
    {
        _aiGuardrails.WithByAlias(guardrailAliases);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured guardrails with this set (replace).
    /// </summary>
    public AIEmbeddingBuilder SetGuardrails(params Guid[] guardrailIds)
    {
        _aiGuardrails.Set(guardrailIds);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured guardrails with this set by alias (replace). Aliases are resolved
    /// to IDs by the service layer.
    /// </summary>
    public AIEmbeddingBuilder SetGuardrails(params string[] guardrailAliases)
    {
        _aiGuardrails.SetByAlias(guardrailAliases);
        return this;
    }

    /// <summary>
    /// Sets additional properties to include in the runtime context.
    /// </summary>
    /// <param name="properties">The additional properties.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder WithAdditionalProperties(IReadOnlyDictionary<string, object?> properties)
    {
        _additionalProperties = properties;
        return this;
    }

    /// <summary>
    /// Marks this inline embedding as a pass-through execution within a parent feature.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddingBuilder AsPassThrough()
    {
        _isPassThrough = true;
        return this;
    }

    internal string? Alias => _alias;
    internal string Name => _name ?? _alias ?? string.Empty;
    internal string? Description => _description;
    internal Guid Id => _id ??= DeterministicGuid.Create(InlineEmbeddingNamespace, _alias ?? string.Empty);
    private Guid? _id;
    internal Guid? ProfileId => _profileId;
    internal string? ProfileAlias => _profileAlias;
    internal EmbeddingGenerationOptions? EmbeddingOptions => _embeddingOptions;
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;
    internal IReadOnlyList<Guid> GuardrailIds => _aiGuardrails.Ids;
    internal IReadOnlyList<string>? GuardrailAliases => _aiGuardrails.Aliases;
    internal IReadOnlyList<Guid> AdditionalGuardrailIds => _aiGuardrails.AdditionalIds;
    internal IReadOnlyList<string>? AdditionalGuardrailAliases => _aiGuardrails.AdditionalAliases;
    internal IReadOnlyDictionary<string, object?>? AdditionalProperties => _additionalProperties;
    internal bool IsPassThrough => _isPassThrough;

    internal void SetResolvedGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _aiGuardrails.SetResolvedIds(guardrailIds);
    internal void SetResolvedAdditionalGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _aiGuardrails.SetResolvedAdditionalIds(guardrailIds);

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Inline embedding alias is required. Call WithAlias() before executing.");
        }
    }

    internal void PopulateContext(AIRuntimeContext context, bool setFeatureMetadata)
    {
        if (setFeatureMetadata)
        {
            context.SetValue(Constants.ContextKeys.FeatureType, Constants.FeatureTypes.InlineEmbedding);
            context.SetValue(Constants.ContextKeys.FeatureId, Id);
            context.SetValue(Constants.ContextKeys.FeatureAlias, Alias);
        }

        _aiGuardrails.WriteToContext(context);

        if (_additionalProperties is not null)
        {
            foreach (var property in _additionalProperties)
            {
                context.SetValue(property.Key, property.Value);
            }
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Utilities;

#pragma warning disable MEAI001 // ImageGenerationOptions is experimental in M.E.AI

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Fluent builder for configuring inline image-generation executions — generations that run purely in code
/// with full observability (notifications, telemetry, duration tracking).
/// </summary>
/// <remarks>
/// <para>
/// Inline image generation is ideal for CMS extensions that need image generation with the full middleware
/// pipeline (auditing, tracking, guardrails, telemetry) without building a full agent.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// var response = await imageGenerationService.GenerateImagesAsync(img => img
///     .WithAlias("hero-banner")
///     .WithProfile("image-profile")
///     .WithGuardrails("content-filter"),   // additive on top of the profile's guardrails
///     "A serene mountain landscape at dawn", cancellationToken);
/// </code>
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public sealed class AIImageGenerationBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5)
    // Different from the other inline builder namespaces to avoid ID collisions
    private static readonly Guid InlineImageGenerationNamespace = new("E1B7C8F4-6A2D-5C9E-8B4F-7A9C3E5D7F1A");

    private string? _alias;
    private string? _name;
    private string? _description;
    private Guid? _profileId;
    private string? _profileAlias;
    private ImageGenerationOptions? _imageGenerationOptions;
    private IEnumerable<AIContent>? _originalImages;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private readonly Guardrails.AIGuardrailBuilderState _aiGuardrails = new();
    private IReadOnlyDictionary<string, object?>? _additionalProperties;
    private bool _isPassThrough;

    /// <summary>
    /// Sets the alias for the inline image generation. Required for auditing and telemetry.
    /// </summary>
    /// <remarks>
    /// The alias is used to generate a deterministic ID, so the same alias always
    /// produces the same generation ID across invocations.
    /// </remarks>
    /// <param name="alias">A unique, URL-safe identifier for this inline image generation.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithAlias(string alias)
    {
        _alias = alias;
        _id = null;
        return this;
    }

    /// <summary>
    /// Sets the display name for the inline image generation. If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description for the inline image generation.
    /// </summary>
    /// <param name="description">The description of what this generation does.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by ID.
    /// If not set, the default image-generation profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        _profileAlias = null;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by alias.
    /// If not set, the default image-generation profile is used.
    /// </summary>
    /// <param name="profileAlias">The profile alias.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithProfile(string profileAlias)
    {
        _profileAlias = profileAlias;
        _profileId = null;
        return this;
    }

    /// <summary>
    /// Sets image-generation options to override profile defaults (size, count, response format, etc.).
    /// </summary>
    /// <param name="options">The image-generation options to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithImageGenerationOptions(ImageGenerationOptions options)
    {
        _imageGenerationOptions = options;
        return this;
    }

    /// <summary>
    /// Sets original images to edit (maskless edit — Tier 2). When supplied, the prompt instructs the model
    /// how to transform the supplied image(s) rather than generating from scratch.
    /// </summary>
    /// <remarks>
    /// Masked editing/outpainting (Tier 3) is not expressible through Microsoft.Extensions.AI's abstraction;
    /// use <see cref="IAIImageGenerationService.CreateImageGeneratorAsync"/> or the tracked-execution helper
    /// to reach the provider-native client via <c>GetService</c>.
    /// </remarks>
    /// <param name="originalImages">The original images to edit.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithOriginalImages(IEnumerable<AIContent> originalImages)
    {
        _originalImages = originalImages;
        return this;
    }

    /// <summary>
    /// Sets context items to populate the runtime context with.
    /// </summary>
    /// <param name="contextItems">The context items.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Adds guardrails on top of the profile's configured guardrails (additive). Use
    /// <see cref="SetGuardrails(Guid[])"/> to fully replace.
    /// </summary>
    public AIImageGenerationBuilder WithGuardrails(params Guid[] guardrailIds)
    {
        _aiGuardrails.With(guardrailIds);
        return this;
    }

    /// <summary>
    /// Adds guardrails by alias on top of the profile's configured guardrails (additive). Aliases are
    /// resolved to IDs by the service layer.
    /// </summary>
    public AIImageGenerationBuilder WithGuardrails(params string[] guardrailAliases)
    {
        _aiGuardrails.WithByAlias(guardrailAliases);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured guardrails with this set (replace).
    /// </summary>
    public AIImageGenerationBuilder SetGuardrails(params Guid[] guardrailIds)
    {
        _aiGuardrails.Set(guardrailIds);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured guardrails with this set by alias (replace). Aliases are resolved
    /// to IDs by the service layer.
    /// </summary>
    public AIImageGenerationBuilder SetGuardrails(params string[] guardrailAliases)
    {
        _aiGuardrails.SetByAlias(guardrailAliases);
        return this;
    }

    /// <summary>
    /// Sets additional properties to include in the runtime context.
    /// </summary>
    /// <param name="properties">The additional properties.</param>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder WithAdditionalProperties(IReadOnlyDictionary<string, object?> properties)
    {
        _additionalProperties = properties;
        return this;
    }

    /// <summary>
    /// Marks this inline image generation as a pass-through execution within a parent feature.
    /// </summary>
    /// <remarks>
    /// When enabled, the inline image generation skips feature metadata (FeatureType/FeatureId/FeatureAlias),
    /// notifications, and duration tracking — the parent feature is responsible for its own observability.
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public AIImageGenerationBuilder AsPassThrough()
    {
        _isPassThrough = true;
        return this;
    }

    /// <summary>
    /// Gets the alias configured on this builder.
    /// </summary>
    internal string? Alias => _alias;

    /// <summary>
    /// Gets the display name, defaulting to alias.
    /// </summary>
    internal string Name => _name ?? _alias ?? string.Empty;

    /// <summary>
    /// Gets the description configured on this builder.
    /// </summary>
    internal string? Description => _description;

    /// <summary>
    /// Gets the deterministic ID derived from the alias. Cached after first access.
    /// </summary>
    internal Guid Id => _id ??= DeterministicGuid.Create(InlineImageGenerationNamespace, _alias ?? string.Empty);
    private Guid? _id;

    /// <summary>
    /// Gets the profile ID configured on this builder.
    /// </summary>
    internal Guid? ProfileId => _profileId;

    /// <summary>
    /// Gets the profile alias configured on this builder, if any.
    /// </summary>
    internal string? ProfileAlias => _profileAlias;

    /// <summary>
    /// Gets the image-generation options configured on this builder.
    /// </summary>
    internal ImageGenerationOptions? ImageGenerationOptions => _imageGenerationOptions;

    /// <summary>
    /// Gets the original images configured on this builder (maskless edit).
    /// </summary>
    internal IEnumerable<AIContent>? OriginalImages => _originalImages;

    /// <summary>
    /// Gets the context items configured on this builder.
    /// </summary>
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;

    internal IReadOnlyList<Guid> GuardrailIds => _aiGuardrails.Ids;
    internal IReadOnlyList<string>? GuardrailAliases => _aiGuardrails.Aliases;
    internal IReadOnlyList<Guid> AdditionalGuardrailIds => _aiGuardrails.AdditionalIds;
    internal IReadOnlyList<string>? AdditionalGuardrailAliases => _aiGuardrails.AdditionalAliases;

    /// <summary>
    /// Gets the additional properties configured on this builder.
    /// </summary>
    internal IReadOnlyDictionary<string, object?>? AdditionalProperties => _additionalProperties;

    /// <summary>
    /// Gets whether this execution is a pass-through within a parent feature.
    /// </summary>
    internal bool IsPassThrough => _isPassThrough;

    /// <summary>
    /// Sets resolved guardrail IDs from alias lookup. Used by the service layer
    /// to resolve aliases before execution.
    /// </summary>
    internal void SetResolvedGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _aiGuardrails.SetResolvedIds(guardrailIds);

    /// <summary>
    /// Sets resolved additional guardrail IDs from alias lookup (additive mode).
    /// </summary>
    internal void SetResolvedAdditionalGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _aiGuardrails.SetResolvedAdditionalIds(guardrailIds);

    /// <summary>
    /// Validates the builder configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the alias is missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Inline image-generation alias is required. Call WithAlias() before executing.");
        }
    }

    /// <summary>
    /// Populates the runtime context with inline image-generation metadata from this builder.
    /// </summary>
    /// <param name="context">The runtime context to populate.</param>
    /// <param name="setFeatureMetadata">
    /// Whether to set feature identity (FeatureType/FeatureId/FeatureAlias).
    /// Pass <c>false</c> when a parent scope already set its own feature identity.
    /// </param>
    internal void PopulateContext(AIRuntimeContext context, bool setFeatureMetadata)
    {
        if (setFeatureMetadata)
        {
            context.SetValue(Constants.ContextKeys.FeatureType, Constants.FeatureTypes.InlineImageGeneration);
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

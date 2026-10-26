using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Fluent builder for configuring inline extraction executions — extractions that run purely in code with
/// full observability (notifications, telemetry, duration tracking).
/// </summary>
/// <remarks>
/// Mirrors <c>AISpeechToTextBuilder</c>, trimmed to what <see cref="ScopedInlineExtractionClient"/>, the
/// executing/executed notifications and <c>AIExtractionService</c> need. No guardrail configuration.
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIExtractionBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5). Distinct from the other inline-feature
    // namespaces (chat/agent/speech-to-text/embedding/image-generation/decision) to avoid ID collisions.
    private static readonly Guid InlineExtractionNamespace = new("5A9D3E7B-2C4F-4B8A-9D1E-6F3A7C5B8E2D");

    private string? _alias;
    private string? _name;
    private Guid? _profileId;
    private string? _profileAlias;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private bool _isPassThrough;
    private Guid? _id;
    private AIExtractionOptions? _options;

    /// <summary>
    /// Sets the alias for the inline extraction. Required for auditing and telemetry.
    /// </summary>
    /// <remarks>
    /// The alias is used to generate a deterministic ID, so the same alias always
    /// produces the same extraction ID across invocations.
    /// </remarks>
    /// <param name="alias">A unique, URL-safe identifier for this inline extraction.</param>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder WithAlias(string alias)
    {
        _alias = alias;
        _id = null;
        return this;
    }

    /// <summary>
    /// Sets the display name for the inline extraction.
    /// If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by ID.
    /// If not set, the default extraction profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        _profileAlias = null;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by alias.
    /// If not set, the default extraction profile is used.
    /// </summary>
    /// <param name="profileAlias">The profile alias.</param>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder WithProfile(string profileAlias)
    {
        _profileAlias = profileAlias;
        _profileId = null;
        return this;
    }

    /// <summary>
    /// Sets context items to populate the runtime context with.
    /// </summary>
    /// <param name="contextItems">The context items.</param>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Sets per-call options (e.g. a model override) for the inline extraction.
    /// </summary>
    /// <param name="options">The per-call extraction options.</param>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder WithExtractionOptions(AIExtractionOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Marks this inline extraction as a pass-through execution within a parent feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the inline extraction skips feature metadata (FeatureType/FeatureId/FeatureAlias),
    /// notifications, and duration tracking — the parent feature is responsible for its own observability.
    /// </para>
    /// <para>
    /// Use this when calling the inline extraction API from within a feature that already manages
    /// its own runtime context scope and notifications.
    /// </para>
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public AIExtractionBuilder AsPassThrough()
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
    /// Gets the deterministic ID derived from the alias. Cached after first access.
    /// </summary>
    internal Guid Id => _id ??= DeterministicGuid.Create(InlineExtractionNamespace, _alias ?? string.Empty);

    /// <summary>
    /// Gets the profile ID configured on this builder.
    /// </summary>
    internal Guid? ProfileId => _profileId;

    /// <summary>
    /// Gets the profile alias configured on this builder, if any.
    /// </summary>
    internal string? ProfileAlias => _profileAlias;

    /// <summary>
    /// Gets the context items configured on this builder.
    /// </summary>
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;

    /// <summary>
    /// Gets whether this execution is a pass-through within a parent feature.
    /// </summary>
    internal bool IsPassThrough => _isPassThrough;

    /// <summary>
    /// Gets the per-call options configured on this builder, if any.
    /// </summary>
    internal AIExtractionOptions? Options => _options;

    /// <summary>
    /// Validates the builder configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the alias is missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Inline extraction alias is required. Call WithAlias() before executing.");
        }
    }

    /// <summary>
    /// Populates the runtime context with inline extraction metadata from this builder.
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
            context.SetValue(Constants.ContextKeys.FeatureType, Constants.FeatureTypes.InlineExtraction);
            context.SetValue(Constants.ContextKeys.FeatureId, Id);
            context.SetValue(Constants.ContextKeys.FeatureAlias, Alias);
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Fluent builder for configuring inline decision executions — decisions that run purely in code with
/// full observability (notifications, telemetry, duration tracking).
/// </summary>
/// <remarks>
/// Mirrors <c>AISpeechToTextBuilder</c>, trimmed to what <see cref="ScopedInlineDecisionClient"/> and the
/// executing/executed notifications need today. No guardrail configuration — <c>SPEC.md</c> does not
/// call for it on the Decision capability. T8 (<c>AIDecisionService</c>) is expected to add whatever
/// further configuration surface it needs on top of this rather than replace it.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class AIDecisionBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5). Distinct from the other inline-feature
    // namespaces (chat/agent/speech-to-text/embedding/image-generation) to avoid ID collisions.
    private static readonly Guid InlineDecisionNamespace = new("2B6E4C1A-7D3F-4A9E-8C5B-1F6A3D8E9B4C");

    private string? _alias;
    private string? _name;
    private Guid? _profileId;
    private string? _profileAlias;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private bool _isPassThrough;
    private Guid? _id;

    /// <summary>
    /// Sets the alias for the inline decision. Required for auditing and telemetry.
    /// </summary>
    /// <remarks>
    /// The alias is used to generate a deterministic ID, so the same alias always
    /// produces the same decision ID across invocations.
    /// </remarks>
    /// <param name="alias">A unique, URL-safe identifier for this inline decision.</param>
    /// <returns>The builder for chaining.</returns>
    public AIDecisionBuilder WithAlias(string alias)
    {
        _alias = alias;
        _id = null;
        return this;
    }

    /// <summary>
    /// Sets the display name for the inline decision.
    /// If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AIDecisionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by ID.
    /// If not set, the default decision profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIDecisionBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        _profileAlias = null;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by alias.
    /// If not set, the default decision profile is used.
    /// </summary>
    /// <param name="profileAlias">The profile alias.</param>
    /// <returns>The builder for chaining.</returns>
    public AIDecisionBuilder WithProfile(string profileAlias)
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
    public AIDecisionBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Marks this inline decision as a pass-through execution within a parent feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the inline decision skips feature metadata (FeatureType/FeatureId/FeatureAlias),
    /// notifications, and duration tracking — the parent feature is responsible for its own observability.
    /// </para>
    /// <para>
    /// Use this when calling the inline decision API from within a feature that already manages
    /// its own runtime context scope and notifications.
    /// </para>
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public AIDecisionBuilder AsPassThrough()
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
    internal Guid Id => _id ??= DeterministicGuid.Create(InlineDecisionNamespace, _alias ?? string.Empty);

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
    /// Validates the builder configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the alias is missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Inline decision alias is required. Call WithAlias() before executing.");
        }
    }

    /// <summary>
    /// Populates the runtime context with inline decision metadata from this builder.
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
            context.SetValue(Constants.ContextKeys.FeatureType, Constants.FeatureTypes.InlineDecision);
            context.SetValue(Constants.ContextKeys.FeatureId, Id);
            context.SetValue(Constants.ContextKeys.FeatureAlias, Alias);
        }
    }
}

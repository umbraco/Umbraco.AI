using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Utilities;

#pragma warning disable MEAI001 // SpeechToTextOptions is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Fluent builder for configuring inline speech-to-text executions — transcriptions that run purely in code
/// with full observability (notifications, telemetry, duration tracking).
/// </summary>
/// <remarks>
/// <para>
/// Inline speech-to-text is ideal for CMS extensions that need audio transcription with the full middleware
/// pipeline (auditing, tracking, guardrails, telemetry) without building a full agent.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// var response = await speechToTextService.TranscribeAsync(stt => stt
///     .WithAlias("voice-notes")
///     .WithProfile("whisper-profile")
///     .WithGuardrails("content-filter"),
///     audioStream, cancellationToken);
/// </code>
/// </remarks>
public sealed class AISpeechToTextBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5)
    // Different from inline chat and inline agent namespaces to avoid ID collisions
    private static readonly Guid InlineSpeechToTextNamespace = new("C9A5B6D3-4E0F-5A8B-9C3D-5F7A2B4D6E8F");

    private string? _alias;
    private string? _name;
    private string? _description;
    private Guid? _profileId;
    private string? _profileAlias;
    private SpeechToTextOptions? _speechToTextOptions;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private IReadOnlyList<Guid> _guardrailIds = [];
    private IReadOnlyList<string>? _guardrailAliases;
    private IReadOnlyDictionary<string, object?>? _additionalProperties;
    private bool _isPassThrough;

    /// <summary>
    /// Sets the alias for the inline speech-to-text. Required for auditing and telemetry.
    /// </summary>
    /// <remarks>
    /// The alias is used to generate a deterministic ID, so the same alias always
    /// produces the same transcription ID across invocations.
    /// </remarks>
    /// <param name="alias">A unique, URL-safe identifier for this inline speech-to-text.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithAlias(string alias)
    {
        _alias = alias;
        _id = null;
        return this;
    }

    /// <summary>
    /// Sets the display name for the inline speech-to-text.
    /// If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description for the inline speech-to-text.
    /// </summary>
    /// <param name="description">The description of what this transcription does.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by ID.
    /// If not set, the default speech-to-text profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        _profileAlias = null;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by alias.
    /// If not set, the default speech-to-text profile is used.
    /// </summary>
    /// <param name="profileAlias">The profile alias.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithProfile(string profileAlias)
    {
        _profileAlias = profileAlias;
        _profileId = null;
        return this;
    }

    /// <summary>
    /// Sets speech-to-text options to override profile defaults (language, model, etc.).
    /// </summary>
    /// <param name="options">The speech-to-text options to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithSpeechToTextOptions(SpeechToTextOptions options)
    {
        _speechToTextOptions = options;
        return this;
    }

    /// <summary>
    /// Sets context items to populate the runtime context with.
    /// </summary>
    /// <param name="contextItems">The context items.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Sets guardrails for safety and compliance checks by ID.
    /// </summary>
    /// <param name="guardrailIds">The guardrail IDs to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithGuardrails(params Guid[] guardrailIds)
    {
        _guardrailIds = guardrailIds;
        _guardrailAliases = null;
        return this;
    }

    /// <summary>
    /// Sets guardrails for safety and compliance checks by alias.
    /// </summary>
    /// <param name="guardrailAliases">The guardrail aliases to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithGuardrails(params string[] guardrailAliases)
    {
        _guardrailAliases = guardrailAliases;
        _guardrailIds = [];
        return this;
    }

    /// <summary>
    /// Sets additional properties to include in the runtime context.
    /// </summary>
    /// <param name="properties">The additional properties.</param>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder WithAdditionalProperties(IReadOnlyDictionary<string, object?> properties)
    {
        _additionalProperties = properties;
        return this;
    }

    /// <summary>
    /// Marks this inline speech-to-text as a pass-through execution within a parent feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the inline speech-to-text skips feature metadata (FeatureType/FeatureId/FeatureAlias),
    /// notifications, and duration tracking — the parent feature is responsible for its own observability.
    /// </para>
    /// <para>
    /// Use this when calling the inline speech-to-text API from within a feature that already manages
    /// its own runtime context scope and notifications.
    /// </para>
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public AISpeechToTextBuilder AsPassThrough()
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
    internal Guid Id => _id ??= DeterministicGuid.Create(InlineSpeechToTextNamespace, _alias ?? string.Empty);
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
    /// Gets the speech-to-text options configured on this builder.
    /// </summary>
    internal SpeechToTextOptions? SpeechToTextOptions => _speechToTextOptions;

    /// <summary>
    /// Gets the context items configured on this builder.
    /// </summary>
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;

    /// <summary>
    /// Gets the guardrail IDs configured on this builder.
    /// </summary>
    internal IReadOnlyList<Guid> GuardrailIds => _guardrailIds;

    /// <summary>
    /// Gets the guardrail aliases configured on this builder, if any.
    /// </summary>
    internal IReadOnlyList<string>? GuardrailAliases => _guardrailAliases;

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
    internal void SetResolvedGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _guardrailIds = guardrailIds;

    /// <summary>
    /// Validates the builder configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the alias is missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Inline speech-to-text alias is required. Call WithAlias() before executing.");
        }
    }

    /// <summary>
    /// Populates the runtime context with inline speech-to-text metadata from this builder.
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
            context.SetValue(Constants.ContextKeys.FeatureType, Constants.FeatureTypes.InlineSpeechToText);
            context.SetValue(Constants.ContextKeys.FeatureId, Id);
            context.SetValue(Constants.ContextKeys.FeatureAlias, Alias);
        }

        if (_guardrailIds.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.GuardrailIdsOverride, _guardrailIds);
        }

        if (_additionalProperties is not null)
        {
            foreach (var property in _additionalProperties)
            {
                context.SetValue(property.Key, property.Value);
            }
        }
    }
}

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Utilities;

namespace Umbraco.AI.Core.InlineChat;

/// <summary>
/// Fluent builder for configuring inline chat executions — chat completions that run purely in code
/// with full observability (notifications, telemetry, duration tracking).
/// </summary>
/// <remarks>
/// <para>
/// Inline chat is ideal for CMS extensions that need chat completions with the full middleware
/// pipeline (auditing, tracking, guardrails, telemetry) without building a full agent.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// </para>
/// <code>
/// var response = await chatService.GetChatResponseAsync(chat => chat
///     .WithAlias("my-summarizer")
///     .WithProfile(profileId)
///     .WithChatOptions(new ChatOptions { Temperature = 0.3f })
///     .WithGuardrails(guardrailId)   // additive on top of the profile's guardrails
///     .WithContexts(contextId),      // additive on top of the profile's contexts
///     messages, cancellationToken);
/// // Use SetGuardrails / SetContexts to replace the profile's configured values.
/// </code>
/// </remarks>
public sealed class AIChatBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5)
    // Different from inline agent namespace to avoid ID collisions
    private static readonly Guid InlineChatNamespace = new("B8F4A5C2-3D9E-4F7A-8B2C-4E6F8A1C3D5E");

    private string? _alias;
    private string? _name;
    private string? _description;
    private Guid? _profileId;
    private string? _profileAlias;
    private ChatOptions? _chatOptions;
    private AIOutputSchema? _outputSchema;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private readonly AIContextBuilderState _aiContexts = new();
    private readonly AIGuardrailBuilderState _aiGuardrails = new();
    private IReadOnlyDictionary<string, object?>? _additionalProperties;
    private bool _isPassThrough;
    private readonly List<string> _toolIds = [];

    /// <summary>
    /// Sets the alias for the inline chat. Required for auditing and telemetry.
    /// </summary>
    /// <remarks>
    /// The alias is used to generate a deterministic ID, so the same alias always
    /// produces the same chat ID across invocations.
    /// </remarks>
    /// <param name="alias">A unique, URL-safe identifier for this inline chat.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithAlias(string alias)
    {
        _alias = alias;
        _id = null;
        return this;
    }

    /// <summary>
    /// Sets the display name for the inline chat.
    /// If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description for the inline chat.
    /// </summary>
    /// <param name="description">The description of what this chat does.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by ID.
    /// If not set, the default chat profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        _profileAlias = null;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration by alias.
    /// If not set, the default chat profile is used.
    /// </summary>
    /// <param name="profileAlias">The profile alias.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithProfile(string profileAlias)
    {
        _profileAlias = profileAlias;
        _profileId = null;
        return this;
    }

    /// <summary>
    /// Sets chat options to override profile defaults (temperature, max tokens, etc.).
    /// </summary>
    /// <param name="options">The chat options to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithChatOptions(ChatOptions options)
    {
        _chatOptions = options;
        return this;
    }

    /// <summary>
    /// Sets an output schema that constrains the chat response to a specific structure.
    /// The schema is applied as <see cref="ChatOptions.ResponseFormat"/> automatically.
    /// </summary>
    /// <param name="schema">The output schema to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithOutputSchema(AIOutputSchema schema)
    {
        _outputSchema = schema;
        return this;
    }

    /// <summary>
    /// Sets context items to populate the runtime context with.
    /// </summary>
    /// <param name="contextItems">The context items.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Adds stored <see cref="Models.AIContext"/> entries on top of the profile's configured contexts
    /// (additive). Use <see cref="SetContexts(Guid[])"/> to fully replace.
    /// </summary>
    public AIChatBuilder WithContexts(params Guid[] contextIds)
    {
        _aiContexts.With(contextIds);
        return this;
    }

    /// <summary>
    /// Adds stored <see cref="Models.AIContext"/> entries by alias on top of the profile's configured
    /// contexts (additive). Aliases are resolved to IDs by the service layer.
    /// </summary>
    public AIChatBuilder WithContexts(params string[] contextAliases)
    {
        _aiContexts.WithByAlias(contextAliases);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured contexts with this set (replace). Pass an empty array to
    /// explicitly use no contexts.
    /// </summary>
    public AIChatBuilder SetContexts(params Guid[] contextIds)
    {
        _aiContexts.Set(contextIds);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured contexts with this set by alias (replace). Aliases are resolved
    /// to IDs by the service layer.
    /// </summary>
    public AIChatBuilder SetContexts(params string[] contextAliases)
    {
        _aiContexts.SetByAlias(contextAliases);
        return this;
    }

    /// <summary>
    /// Adds guardrails on top of the profile's configured guardrails (additive). Use
    /// <see cref="SetGuardrails(Guid[])"/> to fully replace.
    /// </summary>
    public AIChatBuilder WithGuardrails(params Guid[] guardrailIds)
    {
        _aiGuardrails.With(guardrailIds);
        return this;
    }

    /// <summary>
    /// Adds guardrails by alias on top of the profile's configured guardrails (additive). Aliases are
    /// resolved to IDs by the service layer.
    /// </summary>
    public AIChatBuilder WithGuardrails(params string[] guardrailAliases)
    {
        _aiGuardrails.WithByAlias(guardrailAliases);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured guardrails with this set (replace).
    /// </summary>
    public AIChatBuilder SetGuardrails(params Guid[] guardrailIds)
    {
        _aiGuardrails.Set(guardrailIds);
        return this;
    }

    /// <summary>
    /// Replaces the profile's configured guardrails with this set by alias (replace). Aliases are resolved
    /// to IDs by the service layer.
    /// </summary>
    public AIChatBuilder SetGuardrails(params string[] guardrailAliases)
    {
        _aiGuardrails.SetByAlias(guardrailAliases);
        return this;
    }

    /// <summary>
    /// Sets additional properties to include in the runtime context.
    /// </summary>
    /// <param name="properties">The additional properties.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithAdditionalProperties(IReadOnlyDictionary<string, object?> properties)
    {
        _additionalProperties = properties;
        return this;
    }

    /// <summary>
    /// Includes registered AI tools in the chat request by their IDs.
    /// Unknown IDs throw at execution time.
    /// </summary>
    /// <param name="toolIds">The tool IDs to include.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithTools(params string[] toolIds)
    {
        _toolIds.AddRange(toolIds);
        return this;
    }

    /// <summary>
    /// Marks this inline chat as a pass-through execution within a parent feature.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the inline chat skips feature metadata (FeatureType/FeatureId/FeatureAlias),
    /// notifications, and duration tracking — the parent feature (e.g., prompt, agent) is
    /// responsible for its own observability.
    /// </para>
    /// <para>
    /// Use this when calling the inline chat API from within a feature that already manages
    /// its own runtime context scope and notifications.
    /// </para>
    /// </remarks>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder AsPassThrough()
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
    internal Guid Id => _id ??= DeterministicGuid.Create(InlineChatNamespace, _alias ?? string.Empty);
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
    /// Gets the chat options configured on this builder.
    /// </summary>
    internal ChatOptions? ChatOptions => _chatOptions;

    /// <summary>
    /// Gets the output schema configured on this builder.
    /// </summary>
    internal AIOutputSchema? OutputSchema => _outputSchema;

    /// <summary>
    /// Gets the context items configured on this builder.
    /// </summary>
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;

    internal IReadOnlyList<Guid>? ContextIds => _aiContexts.Ids;
    internal IReadOnlyList<string>? ContextAliases => _aiContexts.Aliases;
    internal IReadOnlyList<Guid> AdditionalContextIds => _aiContexts.AdditionalIds;
    internal IReadOnlyList<string>? AdditionalContextAliases => _aiContexts.AdditionalAliases;

    internal IReadOnlyList<Guid> GuardrailIds => _aiGuardrails.Ids;
    internal IReadOnlyList<string>? GuardrailAliases => _aiGuardrails.Aliases;
    internal IReadOnlyList<Guid> AdditionalGuardrailIds => _aiGuardrails.AdditionalIds;
    internal IReadOnlyList<string>? AdditionalGuardrailAliases => _aiGuardrails.AdditionalAliases;

    /// <summary>
    /// Gets the additional properties configured on this builder.
    /// </summary>
    internal IReadOnlyDictionary<string, object?>? AdditionalProperties => _additionalProperties;

    /// <summary>
    /// Gets the tool IDs configured on this builder.
    /// </summary>
    internal IReadOnlyList<string> ToolIds => _toolIds;

    /// <summary>
    /// Gets whether this execution is a pass-through within a parent feature.
    /// </summary>
    internal bool IsPassThrough => _isPassThrough;

    /// <summary>
    /// Sets resolved guardrail IDs from alias lookup (replace mode). Used by the service layer.
    /// </summary>
    internal void SetResolvedGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _aiGuardrails.SetResolvedIds(guardrailIds);

    /// <summary>
    /// Sets resolved additional guardrail IDs from alias lookup (additive mode). Used by the service layer.
    /// </summary>
    internal void SetResolvedAdditionalGuardrailIds(IReadOnlyList<Guid> guardrailIds) => _aiGuardrails.SetResolvedAdditionalIds(guardrailIds);

    /// <summary>
    /// Sets resolved context IDs from alias lookup (replace mode). Used by the service layer.
    /// </summary>
    internal void SetResolvedContextIds(IReadOnlyList<Guid> contextIds) => _aiContexts.SetResolvedIds(contextIds);

    /// <summary>
    /// Sets resolved additional context IDs from alias lookup (additive mode). Used by the service layer.
    /// </summary>
    internal void SetResolvedAdditionalContextIds(IReadOnlyList<Guid> contextIds) => _aiContexts.SetResolvedAdditionalIds(contextIds);

    /// <summary>
    /// Validates the builder configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the alias is missing.</exception>
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Inline chat alias is required. Call WithAlias() before executing.");
        }
    }

    /// <summary>
    /// Populates the runtime context with inline-chat metadata from this builder.
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
            context.SetValue(Constants.ContextKeys.FeatureType, Constants.FeatureTypes.InlineChat);
            context.SetValue(Constants.ContextKeys.FeatureId, Id);
            context.SetValue(Constants.ContextKeys.FeatureAlias, Alias);
        }

        _aiGuardrails.WriteToContext(context);
        _aiContexts.WriteToContext(context);

        if (_chatOptions is not null)
        {
            context.SetValue(Constants.ContextKeys.ChatOptionsOverride, _chatOptions);
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

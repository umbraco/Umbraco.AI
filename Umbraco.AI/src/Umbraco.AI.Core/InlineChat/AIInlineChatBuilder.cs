using Microsoft.Extensions.AI;
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
///     .WithGuardrails(guardrailId),
///     messages, cancellationToken);
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
    private ChatOptions? _chatOptions;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private IReadOnlyList<Guid> _guardrailIds = [];
    private IReadOnlyDictionary<string, object?>? _additionalProperties;
    private bool _isPassThrough;

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
    /// Sets the profile to use for AI model configuration.
    /// If not set, the default chat profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
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
    /// Sets guardrail IDs for safety and compliance checks.
    /// </summary>
    /// <param name="guardrailIds">The guardrail IDs to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AIChatBuilder WithGuardrails(params Guid[] guardrailIds)
    {
        _guardrailIds = guardrailIds;
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
    /// Gets the chat options configured on this builder.
    /// </summary>
    internal ChatOptions? ChatOptions => _chatOptions;

    /// <summary>
    /// Gets the context items configured on this builder.
    /// </summary>
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;

    /// <summary>
    /// Gets the guardrail IDs configured on this builder.
    /// </summary>
    internal IReadOnlyList<Guid> GuardrailIds => _guardrailIds;

    /// <summary>
    /// Gets the additional properties configured on this builder.
    /// </summary>
    internal IReadOnlyDictionary<string, object?>? AdditionalProperties => _additionalProperties;

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

        if (_guardrailIds.Count > 0)
        {
            context.SetValue(Constants.ContextKeys.GuardrailIdsOverride, _guardrailIds);
        }

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

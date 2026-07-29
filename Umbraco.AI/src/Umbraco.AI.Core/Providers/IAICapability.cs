using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core.DependencyInjection;

#pragma warning disable MEAI001 // ISpeechToTextClient / IImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Defining the experimental image-generation capability surface

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Helper methods for capability runtime validation.
/// </summary>
internal static class CapabilityGuards
{
    /// <summary>
    /// Throws if settings are still in unresolved JsonElement form.
    /// This catches cases where callers bypass the configured provider pattern.
    /// </summary>
    internal static void ThrowIfUnresolvedSettings(object? settings, string methodName)
    {
        if (settings is JsonElement)
        {
            throw new InvalidOperationException(
                $"Settings must be resolved before calling {methodName}. " +
                "Use IAIConfiguredProvider from IAIConnectionService.GetConfiguredProviderAsync().");
        }
    }
}

/// <summary>
/// Defines a generic AI capability.
/// </summary>
public interface IAICapability
{
    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    AICapability Kind { get; }

    /// <summary>
    /// Gets the type that represents the provider-declared capability settings for this
    /// capability (e.g. reasoning effort), or <c>null</c> if the capability declares no such extras.
    /// </summary>
    /// <remarks>
    /// This is the profile-level analogue of <see cref="IAIProvider.SettingsType"/> (which describes
    /// connection settings). It is a reflection hook read by the provider/registry to build a schema
    /// and to resolve the stored bag into a typed object; providers do not implement it directly but
    /// instead derive their capability from the two-parameter <c>AIChatCapabilityBase&lt;TSettings, TCapabilitySettings&gt;</c>.
    /// </remarks>
    Type? CapabilitySettingsType => null;

    /// <summary>
    /// Declares which settings the given model does not accept. Defaults to
    /// <see cref="AIModelSettingsSupport.Default"/> — nothing declared, so every setting applies.
    /// </summary>
    /// <param name="modelId">The model ID to describe.</param>
    /// <remarks>
    /// <para>
    /// Support for a setting usually varies by model, not by provider, so this is where a capability
    /// says "reasoning effort does not apply to gpt-4o". The capability bases project the result into
    /// <see cref="AIModelDescriptor.Metadata"/> as the model list is built, so the backoffice can hide
    /// inapplicable settings without a second round trip.
    /// </para>
    /// <para>
    /// Must be a cheap, local, synchronous decision — it runs once per model in the list. The same
    /// predicate should also gate what the capability actually sends, since this declaration only
    /// reaches the UI (see <see cref="AIModelSettingsSupport"/>).
    /// </para>
    /// </remarks>
    AIModelSettingsSupport GetSettingsSupport(string modelId) => AIModelSettingsSupport.Default;

    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines an AI capability with specific settings.
/// </summary>
/// <typeparam name="TSettings"></typeparam>
public interface IAICapability<TSettings> : IAICapability
    where TSettings : class
{ }

/// <summary>
/// Defines an AI capability for chat completions.
/// </summary>
public interface IAIChatCapability : IAICapability
{
    /// <summary>
    /// Creates a chat client with the provided settings.
    /// </summary>
    /// <param name="settings">Provider-specific settings (e.g., API key).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured chat client.</returns>
    Task<IChatClient> CreateClientAsync(object? settings = null, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a chat client with the provided connection settings and resolved, provider-declared
    /// profile settings (e.g. reasoning effort).
    /// </summary>
    /// <param name="settings">Provider-specific connection settings (e.g., API key). Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="capabilitySettings">The resolved, typed capability settings, or <c>null</c> when the profile declares none. Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured chat client.</returns>
    /// <remarks>
    /// Default implementation ignores <paramref name="capabilitySettings"/> and delegates to
    /// <see cref="CreateClientAsync(object?, string?, CancellationToken)"/> so existing capabilities keep working.
    /// The two-parameter <c>AIChatCapabilityBase&lt;TSettings, TCapabilitySettings&gt;</c> overrides this to apply them per request.
    /// </remarks>
    Task<IChatClient> CreateClientAsync(object? settings, object? capabilitySettings, string? modelId, CancellationToken cancellationToken)
        => CreateClientAsync(settings, modelId, cancellationToken);
}

/// <summary>
/// Defines an AI capability for speech-to-text transcription.
/// </summary>
public interface IAISpeechToTextCapability : IAICapability
{
    /// <summary>
    /// Creates a speech-to-text client with the provided settings.
    /// </summary>
    /// <param name="settings">Provider-specific settings (e.g., API key).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured speech-to-text client.</returns>
    Task<ISpeechToTextClient> CreateClientAsync(object? settings = null, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a speech-to-text client with the provided connection settings and resolved,
    /// provider-declared capability settings.
    /// </summary>
    /// <param name="settings">Provider-specific connection settings (e.g., API key). Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="capabilitySettings">The resolved, typed capability settings, or <c>null</c> when the profile declares none. Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured speech-to-text client.</returns>
    /// <remarks>
    /// Default implementation ignores <paramref name="capabilitySettings"/> and delegates to
    /// <see cref="CreateClientAsync(object?, string?, CancellationToken)"/> so existing capabilities keep working.
    /// The two-parameter <c>AISpeechToTextCapabilityBase&lt;TSettings, TCapabilitySettings&gt;</c> overrides this to apply them per request.
    /// </remarks>
    Task<ISpeechToTextClient> CreateClientAsync(object? settings, object? capabilitySettings, string? modelId, CancellationToken cancellationToken)
        => CreateClientAsync(settings, modelId, cancellationToken);
}

/// <summary>
/// Defines an AI capability for generating embeddings.
/// </summary>
public interface IAIEmbeddingCapability : IAICapability
{
    /// <summary>
    /// Creates an embedding generator with the provided settings.
    /// </summary>
    /// <param name="settings">Provider-specific settings (e.g., API key).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured embedding generator.</returns>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(object? settings, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an embedding generator with the provided connection settings and resolved,
    /// provider-declared capability settings.
    /// </summary>
    /// <param name="settings">Provider-specific connection settings (e.g., API key). Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="capabilitySettings">The resolved, typed capability settings, or <c>null</c> when the profile declares none. Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured embedding generator.</returns>
    /// <remarks>
    /// Default implementation ignores <paramref name="capabilitySettings"/> and delegates to
    /// <see cref="CreateGeneratorAsync(object?, string?, CancellationToken)"/> so existing capabilities keep working.
    /// The two-parameter <c>AIEmbeddingCapabilityBase&lt;TSettings, TCapabilitySettings&gt;</c> overrides this to apply them per request.
    /// </remarks>
    Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        object? settings,
        object? capabilitySettings,
        string? modelId,
        CancellationToken cancellationToken)
        => CreateGeneratorAsync(settings, modelId, cancellationToken);
}

/// <summary>
/// Defines an AI capability for image generation (text-to-image and image editing).
/// </summary>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public interface IAIImageGeneratorCapability : IAICapability
{
    /// <summary>
    /// Creates an image generator with the provided settings.
    /// </summary>
    /// <param name="settings">Provider-specific settings (e.g., API key).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured image generator.</returns>
    Task<IImageGenerator> CreateGeneratorAsync(object? settings = null, string? modelId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an image generator with the provided connection settings and resolved, provider-declared
    /// capability settings.
    /// </summary>
    /// <param name="settings">Provider-specific connection settings (e.g., API key). Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="capabilitySettings">The resolved, typed capability settings, or <c>null</c> when the profile declares none. Must be resolved (not a raw <see cref="JsonElement"/>).</param>
    /// <param name="modelId">Optional model ID to use. If null, the provider's default model is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured image generator.</returns>
    /// <remarks>
    /// Default implementation ignores <paramref name="capabilitySettings"/> and delegates to
    /// <see cref="CreateGeneratorAsync(object?, string?, CancellationToken)"/> so existing capabilities keep working.
    /// The two-parameter <c>AIImageGeneratorCapabilityBase&lt;TSettings, TCapabilitySettings&gt;</c> overrides this to apply them per request.
    /// </remarks>
    Task<IImageGenerator> CreateGeneratorAsync(
        object? settings,
        object? capabilitySettings,
        string? modelId,
        CancellationToken cancellationToken)
        => CreateGeneratorAsync(settings, modelId, cancellationToken);
}

/// <summary>
/// Base implementation of an AI capability.
/// </summary>
public abstract class AICapabilityBase(IAIProvider provider) : IAICapability
{
    /// <summary>
    /// Gets or sets the AI provider this capability belongs to.
    /// </summary>
    protected IAIProvider Provider { get; set; } = provider;

    /// <summary>
    /// A logger for this capability, resolved lazily through the service locator.
    /// </summary>
    /// <remarks>
    /// Capabilities are constructed by the provider rather than by DI in every path (plain activation is
    /// supported), so the locator is the only way the base can log without changing every provider's
    /// constructor. Null before startup and in unit tests, which is why every use is null-conditional.
    /// </remarks>
    protected ILogger? Logger => _logger ??= StaticServiceProvider.Instance
        ?.GetService<ILoggerFactory>()
        ?.CreateLogger(GetType());

    private ILogger? _logger;

    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    public abstract AICapability Kind { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Implements the interface member as a real virtual class member (rather than relying on the
    /// interface default) so the two-parameter capability bases can <c>override</c> it and interface
    /// dispatch resolves to that override. Defaults to <c>null</c> (no profile settings).
    /// </remarks>
    public virtual Type? CapabilitySettingsType => null;

    /// <inheritdoc />
    public virtual AIModelSettingsSupport GetSettingsSupport(string modelId) => AIModelSettingsSupport.Default;

    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<AIModelDescriptor>> IAICapability.GetModelsAsync(object? settings, CancellationToken cancellationToken)
    {
        var models = await GetModelsAsync(cancellationToken).ConfigureAwait(false);

        // Fold the capability's per-model setting declarations into each descriptor's metadata so the
        // model list doubles as the applicability source for the profile editor.
        return CapabilitySettingsSupportProjection.Apply(this, models);
    }
}

/// <summary>
/// Base implementation of an AI capability with specific settings.
/// </summary>
public abstract class AICapabilityBase<TSettings>(IAIProvider provider) : IAICapability
    where TSettings : class
{
    /// <summary>
    /// Gets or sets the AI provider this capability belongs to.
    /// </summary>
    protected IAIProvider Provider { get; set; } = provider;

    /// <summary>
    /// A logger for this capability, resolved lazily through the service locator.
    /// </summary>
    /// <remarks>
    /// Capabilities are constructed by the provider rather than by DI in every path (plain activation is
    /// supported), so the locator is the only way the base can log without changing every provider's
    /// constructor. Null before startup and in unit tests, which is why every use is null-conditional.
    /// </remarks>
    protected ILogger? Logger => _logger ??= StaticServiceProvider.Instance
        ?.GetService<ILoggerFactory>()
        ?.CreateLogger(GetType());

    private ILogger? _logger;

    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    public abstract AICapability Kind { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Implements the interface member as a real virtual class member (rather than relying on the
    /// interface default) so the two-parameter capability bases can <c>override</c> it and interface
    /// dispatch resolves to that override. Defaults to <c>null</c> (no profile settings).
    /// </remarks>
    public virtual Type? CapabilitySettingsType => null;

    /// <inheritdoc />
    public virtual AIModelSettingsSupport GetSettingsSupport(string modelId) => AIModelSettingsSupport.Default;

    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(TSettings settings, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<AIModelDescriptor>> IAICapability.GetModelsAsync(object? settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(GetModelsAsync));

        var models = await GetModelsAsync((TSettings)settings, cancellationToken).ConfigureAwait(false);

        // Fold the capability's per-model setting declarations into each descriptor's metadata so the
        // model list doubles as the applicability source for the profile editor.
        return CapabilitySettingsSupportProjection.Apply(this, models);
    }
}

/// <summary>
/// Base implementation of an AI chat capability.
/// </summary>
public abstract class AIChatCapabilityBase(IAIProvider provider) : AICapabilityBase(provider), IAIChatCapability
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.Chat;

    /// <summary>
    /// Creates a chat client with the specified model.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured chat client.</returns>
    protected virtual IChatClient CreateClient(string? modelId)
    {
        throw new NotImplementedException("CreateClient must be implemented by chat capability providers.");
    }

    /// <summary>
    /// Creates a chat client with the specified model, asynchronously.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured chat client.</returns>
    protected virtual Task<IChatClient> CreateClientAsync(string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateClient(modelId));
    }

    async Task<IChatClient> IAIChatCapability.CreateClientAsync(object? settings, string? modelId, CancellationToken cancellationToken)
    {
        var inner = await CreateClientAsync(modelId, cancellationToken).ConfigureAwait(false);

        // Enforces this capability's own per-model declaration, so what the editor is told and what the
        // request carries cannot disagree. See DeclaredSettingsChatClient.
        return new DeclaredSettingsChatClient(inner, this, modelId, Logger);
    }
}

/// <summary>
/// Base implementation of an AI chat capability with specific settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific settings type.</typeparam>
public abstract class AIChatCapabilityBase<TSettings>(IAIProvider provider) : AICapabilityBase<TSettings>(provider), IAICapability<TSettings>, IAIChatCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.Chat;

    /// <summary>
    /// Creates a chat client with the provided settings and model.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured chat client.</returns>
    protected virtual IChatClient CreateClient(TSettings settings, string? modelId)
    {
        throw new NotImplementedException("CreateClient must be implemented by chat capability providers.");
    }

    /// <summary>
    /// Creates a chat client with the provided settings and model, asynchronously.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured chat client.</returns>
    protected virtual Task<IChatClient> CreateClientAsync(TSettings settings, string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateClient(settings, modelId));
    }

    /// <inheritdoc />
    Task<IChatClient> IAIChatCapability.CreateClientAsync(object? settings, string? modelId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        return CreateDeclarationEnforcingClientAsync((TSettings)settings, modelId, cancellationToken);
    }

    /// <summary>
    /// Builds the provider's client and wraps it so this capability's per-model declaration is enforced on
    /// every request.
    /// </summary>
    /// <remarks>
    /// Shared with the two-parameter base, which needs the same wrapping beneath its capability-settings
    /// decorator. Wrapped innermost, so no caller can route around it.
    /// </remarks>
    internal async Task<IChatClient> CreateDeclarationEnforcingClientAsync(
        TSettings settings,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var inner = await CreateClientAsync(settings, modelId, cancellationToken).ConfigureAwait(false);

        return new DeclaredSettingsChatClient(inner, this, modelId, Logger);
    }
}

/// <summary>
/// Base implementation of an AI chat capability with both provider-specific connection settings and
/// provider-declared capability settings (e.g. reasoning effort).
/// </summary>
/// <typeparam name="TSettings">The provider-specific connection settings type.</typeparam>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type (a POCO with <c>[AIField]</c> properties).</typeparam>
/// <remarks>
/// Derive from this (instead of <see cref="AIChatCapabilityBase{TSettings}"/>) to let a provider surface
/// extra per-profile settings. The base exposes the schema hook (<see cref="CapabilitySettingsType"/>) and
/// applies the resolved settings to every request's <see cref="ChatOptions"/> via <see cref="ApplyCapabilitySettings"/>;
/// the provider implements only that typed translation.
/// </remarks>
public abstract class AIChatCapabilityBase<TSettings, TCapabilitySettings>(IAIProvider provider)
    : AIChatCapabilityBase<TSettings>(provider), IAIChatCapability
    where TSettings : class
    where TCapabilitySettings : class, new()
{
    /// <inheritdoc />
    public sealed override Type? CapabilitySettingsType => typeof(TCapabilitySettings);

    /// <summary>
    /// Applies the resolved capability settings onto a request's <see cref="ChatOptions"/>.
    /// Called for every request. Implementations should no-op when a value is not set.
    /// </summary>
    /// <param name="capabilitySettings">The resolved, typed capability settings for the profile.</param>
    /// <param name="modelId">
    /// The model the request will run against — the caller's <see cref="ChatOptions.ModelId"/> when set,
    /// otherwise the model the client was created for. <c>null</c> only when neither is known.
    /// </param>
    /// <param name="options">The chat options for the current request (safe to mutate; it is a per-request copy).</param>
    /// <remarks>
    /// Gate on <paramref name="modelId"/> with the same predicate used by
    /// <see cref="IAICapability.GetSettingsSupport"/>: hiding a setting in the editor does not stop a
    /// profile saved before a model change, an alias-driven API caller, or a direct
    /// <see cref="IChatClient"/> consumer from reaching here with a value the model rejects.
    /// </remarks>
    protected abstract void ApplyCapabilitySettings(
        TCapabilitySettings capabilitySettings,
        string? modelId,
        ChatOptions options);

    /// <inheritdoc />
    async Task<IChatClient> IAIChatCapability.CreateClientAsync(
        object? settings,
        object? capabilitySettings,
        string? modelId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        CapabilityGuards.ThrowIfUnresolvedSettings(capabilitySettings, nameof(CreateClient));

        // Build the underlying client from connection settings only (unchanged provider path), already
        // wrapped so the per-model declaration is enforced.
        var inner = await CreateDeclarationEnforcingClientAsync((TSettings)settings, modelId, cancellationToken)
            .ConfigureAwait(false);

        // Wrap so the provider-declared capability settings are applied to every request. When the
        // profile declares none (or a different capability's settings), return the client untouched.
        return capabilitySettings is TCapabilitySettings typed
            ? new CapabilitySettingsChatClient<TCapabilitySettings>(inner, typed, modelId, ApplyCapabilitySettings)
            : inner;
    }
}

/// <summary>
/// Base implementation of an AI embedding capability.
/// </summary>
public abstract class AIEmbeddingCapabilityBase(IAIProvider provider) : AICapabilityBase(provider), IAIEmbeddingCapability
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.Embedding;

    /// <summary>
    /// Creates an embedding generator with the specified model.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured embedding generator.</returns>
    protected virtual IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(string? modelId)
    {
        throw new NotImplementedException("CreateGenerator must be implemented by embedding capability providers.");
    }

    /// <summary>
    /// Creates an embedding generator with the specified model, asynchronously.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured embedding generator.</returns>
    protected virtual Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateGenerator(modelId));
    }

    /// <inheritdoc />
    async Task<IEmbeddingGenerator<string, Embedding<float>>> IAIEmbeddingCapability.CreateGeneratorAsync(object? settings, string? modelId, CancellationToken cancellationToken)
    {
        var inner = await CreateGeneratorAsync(modelId, cancellationToken).ConfigureAwait(false);

        return new DeclaredSettingsEmbeddingGenerator(inner, this, modelId, Logger);
    }
}

/// <summary>
/// Base implementation of an AI embedding capability with specific settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific settings type.</typeparam>
public abstract class AIEmbeddingCapabilityBase<TSettings>(IAIProvider provider) : AICapabilityBase<TSettings>(provider), IAICapability<TSettings>, IAIEmbeddingCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.Embedding;

    /// <summary>
    /// Creates an embedding generator with the provided settings and model.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured embedding generator.</returns>
    protected virtual IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(TSettings settings, string? modelId)
    {
        throw new NotImplementedException("CreateGenerator must be implemented by embedding capability providers.");
    }

    /// <summary>
    /// Creates an embedding generator with the provided settings and model, asynchronously.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured embedding generator.</returns>
    protected virtual Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(TSettings settings, string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateGenerator(settings, modelId));
    }

    /// <inheritdoc />
    Task<IEmbeddingGenerator<string, Embedding<float>>> IAIEmbeddingCapability.CreateGeneratorAsync(object? settings, string? modelId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        return CreateDeclarationEnforcingGeneratorAsync((TSettings)settings, modelId, cancellationToken);
    }

    /// <summary>
    /// Builds the provider's generator and wraps it so this capability's per-model declaration is enforced
    /// on every request.
    /// </summary>
    internal async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateDeclarationEnforcingGeneratorAsync(
        TSettings settings,
        string? modelId,
        CancellationToken cancellationToken)
    {
        var inner = await CreateGeneratorAsync(settings, modelId, cancellationToken).ConfigureAwait(false);

        return new DeclaredSettingsEmbeddingGenerator(inner, this, modelId, Logger);
    }
}

/// <summary>
/// Base implementation of an AI embedding capability with both provider-specific connection settings and
/// provider-declared capability settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific connection settings type.</typeparam>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type (a POCO with <c>[AIField]</c> properties).</typeparam>
/// <remarks>
/// Derive from this (instead of <see cref="AIEmbeddingCapabilityBase{TSettings}"/>) to let a provider
/// surface extra per-profile settings. The base exposes the schema hook (<see cref="CapabilitySettingsType"/>)
/// and applies the resolved settings to every request's <see cref="EmbeddingGenerationOptions"/> via
/// <see cref="ApplyCapabilitySettings"/>; the provider implements only that typed translation.
/// </remarks>
public abstract class AIEmbeddingCapabilityBase<TSettings, TCapabilitySettings>(IAIProvider provider)
    : AIEmbeddingCapabilityBase<TSettings>(provider), IAIEmbeddingCapability
    where TSettings : class
    where TCapabilitySettings : class, new()
{
    /// <inheritdoc />
    public sealed override Type? CapabilitySettingsType => typeof(TCapabilitySettings);

    /// <summary>
    /// Applies the resolved capability settings onto a request's <see cref="EmbeddingGenerationOptions"/>.
    /// Called for every request. Implementations should no-op when a value is not set.
    /// </summary>
    /// <param name="capabilitySettings">The resolved, typed capability settings for the profile.</param>
    /// <param name="modelId">
    /// The model the request will run against — the caller's <see cref="EmbeddingGenerationOptions.ModelId"/>
    /// when set, otherwise the model the generator was created for. <c>null</c> only when neither is known.
    /// </param>
    /// <param name="options">The options for the current request (safe to mutate; it is a per-request copy).</param>
    /// <remarks>
    /// Gate on <paramref name="modelId"/> with the same predicate used by
    /// <see cref="IAICapability.GetSettingsSupport"/>: hiding a setting in the editor does not stop a
    /// profile saved before a model change, or a direct
    /// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> consumer, from reaching here with a value the
    /// model rejects.
    /// </remarks>
    protected abstract void ApplyCapabilitySettings(
        TCapabilitySettings capabilitySettings,
        string? modelId,
        EmbeddingGenerationOptions options);

    /// <inheritdoc />
    async Task<IEmbeddingGenerator<string, Embedding<float>>> IAIEmbeddingCapability.CreateGeneratorAsync(
        object? settings,
        object? capabilitySettings,
        string? modelId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        CapabilityGuards.ThrowIfUnresolvedSettings(capabilitySettings, nameof(CreateGenerator));

        // Build the underlying generator from connection settings only (unchanged provider path), already
        // wrapped so the per-model declaration is enforced.
        var inner = await CreateDeclarationEnforcingGeneratorAsync((TSettings)settings, modelId, cancellationToken)
            .ConfigureAwait(false);

        // Wrap so the provider-declared capability settings are applied to every request. When the
        // profile declares none (or a different capability's settings), return the generator untouched.
        return capabilitySettings is TCapabilitySettings typed
            ? new CapabilitySettingsEmbeddingGenerator<TCapabilitySettings>(inner, typed, modelId, ApplyCapabilitySettings)
            : inner;
    }
}

/// <summary>
/// Base implementation of an AI speech-to-text capability.
/// </summary>
public abstract class AISpeechToTextCapabilityBase(IAIProvider provider) : AICapabilityBase(provider), IAISpeechToTextCapability
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.SpeechToText;

    /// <summary>
    /// Creates a speech-to-text client with the specified model.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured speech-to-text client.</returns>
    protected virtual ISpeechToTextClient CreateClient(string? modelId)
    {
        throw new NotImplementedException("CreateClient must be implemented by speech-to-text capability providers.");
    }

    /// <summary>
    /// Creates a speech-to-text client with the specified model, asynchronously.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured speech-to-text client.</returns>
    protected virtual Task<ISpeechToTextClient> CreateClientAsync(string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateClient(modelId));
    }

    Task<ISpeechToTextClient> IAISpeechToTextCapability.CreateClientAsync(object? settings, string? modelId, CancellationToken cancellationToken)
        => CreateClientAsync(modelId, cancellationToken);
}

/// <summary>
/// Base implementation of an AI speech-to-text capability with specific settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific settings type.</typeparam>
public abstract class AISpeechToTextCapabilityBase<TSettings>(IAIProvider provider) : AICapabilityBase<TSettings>(provider), IAICapability<TSettings>, IAISpeechToTextCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.SpeechToText;

    /// <summary>
    /// Creates a speech-to-text client with the provided settings and model.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured speech-to-text client.</returns>
    protected virtual ISpeechToTextClient CreateClient(TSettings settings, string? modelId)
    {
        throw new NotImplementedException("CreateClient must be implemented by speech-to-text capability providers.");
    }

    /// <summary>
    /// Creates a speech-to-text client with the provided settings and model, asynchronously.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured speech-to-text client.</returns>
    protected virtual Task<ISpeechToTextClient> CreateClientAsync(TSettings settings, string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateClient(settings, modelId));
    }

    /// <inheritdoc />
    Task<ISpeechToTextClient> IAISpeechToTextCapability.CreateClientAsync(object? settings, string? modelId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        return CreateClientAsync((TSettings)settings, modelId, cancellationToken);
    }
}

/// <summary>
/// Base implementation of an AI speech-to-text capability with both provider-specific connection settings
/// and provider-declared capability settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific connection settings type.</typeparam>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type (a POCO with <c>[AIField]</c> properties).</typeparam>
/// <remarks>
/// Derive from this (instead of <see cref="AISpeechToTextCapabilityBase{TSettings}"/>) to let a provider
/// surface extra per-profile settings. The base exposes the schema hook (<see cref="CapabilitySettingsType"/>)
/// and applies the resolved settings to every request's <see cref="SpeechToTextOptions"/> via
/// <see cref="ApplyCapabilitySettings"/>; the provider implements only that typed translation.
/// </remarks>
public abstract class AISpeechToTextCapabilityBase<TSettings, TCapabilitySettings>(IAIProvider provider)
    : AISpeechToTextCapabilityBase<TSettings>(provider), IAISpeechToTextCapability
    where TSettings : class
    where TCapabilitySettings : class, new()
{
    /// <inheritdoc />
    public sealed override Type? CapabilitySettingsType => typeof(TCapabilitySettings);

    /// <summary>
    /// Applies the resolved capability settings onto a request's <see cref="SpeechToTextOptions"/>.
    /// Called for every request. Implementations should no-op when a value is not set.
    /// </summary>
    /// <param name="capabilitySettings">The resolved, typed capability settings for the profile.</param>
    /// <param name="modelId">
    /// The model the request will run against — the caller's <see cref="SpeechToTextOptions.ModelId"/> when
    /// set, otherwise the model the client was created for. <c>null</c> only when neither is known.
    /// </param>
    /// <param name="options">The options for the current request (safe to mutate; it is a per-request copy).</param>
    /// <remarks>
    /// Gate on <paramref name="modelId"/> with the same predicate used by
    /// <see cref="IAICapability.GetSettingsSupport"/>: hiding a setting in the editor does not stop a
    /// profile saved before a model change, or a direct <see cref="ISpeechToTextClient"/> consumer, from
    /// reaching here with a value the model rejects.
    /// </remarks>
    protected abstract void ApplyCapabilitySettings(
        TCapabilitySettings capabilitySettings,
        string? modelId,
        SpeechToTextOptions options);

    /// <inheritdoc />
    async Task<ISpeechToTextClient> IAISpeechToTextCapability.CreateClientAsync(
        object? settings,
        object? capabilitySettings,
        string? modelId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        CapabilityGuards.ThrowIfUnresolvedSettings(capabilitySettings, nameof(CreateClient));

        // Build the underlying client from connection settings only (unchanged provider path). No core
        // filter here: speech-to-text has no cross-provider request option a capability can declare
        // unsupported, so there is nothing for the declaration to strip.
        var inner = await CreateClientAsync((TSettings)settings, modelId, cancellationToken)
            .ConfigureAwait(false);

        // Wrap so the provider-declared capability settings are applied to every request. When the
        // profile declares none (or a different capability's settings), return the client untouched.
        return capabilitySettings is TCapabilitySettings typed
            ? new CapabilitySettingsSpeechToTextClient<TCapabilitySettings>(inner, typed, modelId, ApplyCapabilitySettings)
            : inner;
    }
}

/// <summary>
/// Base implementation of an AI image-generation capability.
/// </summary>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public abstract class AIImageGeneratorCapabilityBase(IAIProvider provider) : AICapabilityBase(provider), IAIImageGeneratorCapability
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.ImageGeneration;

    /// <summary>
    /// Creates an image generator with the specified model.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured image generator.</returns>
    protected virtual IImageGenerator CreateGenerator(string? modelId)
    {
        throw new NotImplementedException("CreateGenerator must be implemented by image-generation capability providers.");
    }

    /// <summary>
    /// Creates an image generator with the specified model, asynchronously.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured image generator.</returns>
    protected virtual Task<IImageGenerator> CreateGeneratorAsync(string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateGenerator(modelId));
    }

    Task<IImageGenerator> IAIImageGeneratorCapability.CreateGeneratorAsync(object? settings, string? modelId, CancellationToken cancellationToken)
        => CreateGeneratorAsync(modelId, cancellationToken);
}

/// <summary>
/// Base implementation of an AI image-generation capability with specific settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific settings type.</typeparam>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public abstract class AIImageGeneratorCapabilityBase<TSettings>(IAIProvider provider) : AICapabilityBase<TSettings>(provider), IAICapability<TSettings>, IAIImageGeneratorCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AICapability Kind => AICapability.ImageGeneration;

    /// <summary>
    /// Creates an image generator with the provided settings and model.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured image generator.</returns>
    protected virtual IImageGenerator CreateGenerator(TSettings settings, string? modelId)
    {
        throw new NotImplementedException("CreateGenerator must be implemented by image-generation capability providers.");
    }

    /// <summary>
    /// Creates an image generator with the provided settings and model, asynchronously.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A configured image generator.</returns>
    protected virtual Task<IImageGenerator> CreateGeneratorAsync(TSettings settings, string? modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateGenerator(settings, modelId));
    }

    /// <inheritdoc />
    Task<IImageGenerator> IAIImageGeneratorCapability.CreateGeneratorAsync(object? settings, string? modelId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        return CreateGeneratorAsync((TSettings)settings, modelId, cancellationToken);
    }
}

/// <summary>
/// Base implementation of an AI image-generation capability with both provider-specific connection
/// settings and provider-declared capability settings (e.g. a quality or style hint).
/// </summary>
/// <typeparam name="TSettings">The provider-specific connection settings type.</typeparam>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type (a POCO with <c>[AIField]</c> properties).</typeparam>
/// <remarks>
/// Derive from this (instead of <see cref="AIImageGeneratorCapabilityBase{TSettings}"/>) to let a provider
/// surface extra per-profile settings. The base exposes the schema hook (<see cref="CapabilitySettingsType"/>)
/// and applies the resolved settings to every request's <see cref="ImageGenerationOptions"/> via
/// <see cref="ApplyCapabilitySettings"/>; the provider implements only that typed translation.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public abstract class AIImageGeneratorCapabilityBase<TSettings, TCapabilitySettings>(IAIProvider provider)
    : AIImageGeneratorCapabilityBase<TSettings>(provider), IAIImageGeneratorCapability
    where TSettings : class
    where TCapabilitySettings : class, new()
{
    /// <inheritdoc />
    public sealed override Type? CapabilitySettingsType => typeof(TCapabilitySettings);

    /// <summary>
    /// Applies the resolved capability settings onto a request's <see cref="ImageGenerationOptions"/>.
    /// Called for every request. Implementations should no-op when a value is not set.
    /// </summary>
    /// <param name="capabilitySettings">The resolved, typed capability settings for the profile.</param>
    /// <param name="modelId">
    /// The model the request will run against — the caller's <see cref="ImageGenerationOptions.ModelId"/>
    /// when set, otherwise the model the generator was created for. <c>null</c> only when neither is known.
    /// </param>
    /// <param name="options">The options for the current request (safe to mutate; it is a per-request copy).</param>
    /// <remarks>
    /// Gate on <paramref name="modelId"/> with the same predicate used by
    /// <see cref="IAICapability.GetSettingsSupport"/>: hiding a setting in the editor does not stop a
    /// profile saved before a model change, or a direct <see cref="IImageGenerator"/> consumer, from
    /// reaching here with a value the model rejects.
    /// </remarks>
    protected abstract void ApplyCapabilitySettings(
        TCapabilitySettings capabilitySettings,
        string? modelId,
        ImageGenerationOptions options);

    /// <inheritdoc />
    async Task<IImageGenerator> IAIImageGeneratorCapability.CreateGeneratorAsync(
        object? settings,
        object? capabilitySettings,
        string? modelId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        CapabilityGuards.ThrowIfUnresolvedSettings(capabilitySettings, nameof(CreateGenerator));

        // Build the underlying generator from connection settings only (unchanged provider path). No core
        // filter here: image sizes are enumerated per model rather than declared unsupported, so there is
        // nothing for the declaration to strip.
        var inner = await CreateGeneratorAsync((TSettings)settings, modelId, cancellationToken)
            .ConfigureAwait(false);

        // Wrap so the provider-declared capability settings are applied to every request. When the
        // profile declares none (or a different capability's settings), return the generator untouched.
        return capabilitySettings is TCapabilitySettings typed
            ? new CapabilitySettingsImageGenerator<TCapabilitySettings>(inner, typed, modelId, ApplyCapabilitySettings)
            : inner;
    }
}

using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;

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
    /// <returns>A configured chat client.</returns>
    IChatClient CreateClient(object? settings = null, string? modelId = null);
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
    /// <returns>A configured embedding generator.</returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings, string? modelId = null);
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
    /// Gets the kind of AI capability.
    /// </summary>
    public abstract AICapability Kind { get; }
    
    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<AIModelDescriptor>> IAICapability.GetModelsAsync(object? settings, CancellationToken cancellationToken)
        => GetModelsAsync(cancellationToken);
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
    /// Gets the kind of AI capability.
    /// </summary>
    public abstract AICapability Kind { get; }
    
    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(TSettings settings, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<AIModelDescriptor>> IAICapability.GetModelsAsync(object? settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(GetModelsAsync));
        return GetModelsAsync((TSettings)settings, cancellationToken);
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
    protected abstract IChatClient CreateClient(string? modelId);

    IChatClient IAIChatCapability.CreateClient(object? settings, string? modelId)
        => CreateClient(modelId);
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
    protected abstract IChatClient CreateClient(TSettings settings, string? modelId);

    /// <inheritdoc />
    IChatClient IAIChatCapability.CreateClient(object? settings, string? modelId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        return CreateClient((TSettings)settings, modelId);
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
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(string? modelId);

    /// <inheritdoc />
    IEmbeddingGenerator<string, Embedding<float>> IAIEmbeddingCapability.CreateGenerator(object? settings, string? modelId)
        => CreateGenerator(modelId);
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
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(TSettings settings, string? modelId);

    /// <inheritdoc />
    IEmbeddingGenerator<string, Embedding<float>> IAIEmbeddingCapability.CreateGenerator(object? settings, string? modelId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        return CreateGenerator((TSettings)settings, modelId);
    }
}
using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Providers;

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
                "Use IAiConfiguredProvider from IAiConnectionService.GetConfiguredProviderAsync().");
        }
    }
}

/// <summary>
/// Defines a generic AI capability.
/// </summary>
public interface IAiCapability
{
    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    AiCapability Kind { get; }
    
    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines an AI capability with specific settings.
/// </summary>
/// <typeparam name="TSettings"></typeparam>
public interface IAiCapability<TSettings> : IAiCapability
    where TSettings : class
{ }

/// <summary>
/// Defines an AI capability for chat completions.
/// </summary>
public interface IAiChatCapability : IAiCapability
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
public interface IAiEmbeddingCapability : IAiCapability
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
public abstract class AiCapabilityBase(IAiProvider provider) : IAiCapability
{
    /// <summary>
    /// Gets or sets the AI provider this capability belongs to.
    /// </summary>
    protected IAiProvider Provider { get; set; } = provider;
    
    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    public abstract AiCapability Kind { get; }
    
    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<AiModelDescriptor>> IAiCapability.GetModelsAsync(object? settings, CancellationToken cancellationToken)
        => GetModelsAsync(cancellationToken);
}

/// <summary>
/// Base implementation of an AI capability with specific settings.
/// </summary>
public abstract class AiCapabilityBase<TSettings>(IAiProvider provider) : IAiCapability
    where TSettings : class
{
    /// <summary>
    /// Gets or sets the AI provider this capability belongs to.
    /// </summary>
    protected IAiProvider Provider { get; set; } = provider;
    
    /// <summary>
    /// Gets the kind of AI capability.
    /// </summary>
    public abstract AiCapability Kind { get; }
    
    /// <summary>
    /// Gets the available AI models for this capability.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected abstract Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(TSettings settings, CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<AiModelDescriptor>> IAiCapability.GetModelsAsync(object? settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(GetModelsAsync));
        return GetModelsAsync((TSettings)settings, cancellationToken);
    }
}

/// <summary>
/// Base implementation of an AI chat capability.
/// </summary>
public abstract class AiChatCapabilityBase(IAiProvider provider) : AiCapabilityBase(provider), IAiChatCapability
{
    /// <inheritdoc />
    public override AiCapability Kind => AiCapability.Chat;

    /// <summary>
    /// Creates a chat client with the specified model.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured chat client.</returns>
    protected abstract IChatClient CreateClient(string? modelId);

    IChatClient IAiChatCapability.CreateClient(object? settings, string? modelId)
        => CreateClient(modelId);
}

/// <summary>
/// Base implementation of an AI chat capability with specific settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific settings type.</typeparam>
public abstract class AiChatCapabilityBase<TSettings>(IAiProvider provider) : AiCapabilityBase<TSettings>(provider), IAiCapability<TSettings>, IAiChatCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AiCapability Kind => AiCapability.Chat;

    /// <summary>
    /// Creates a chat client with the provided settings and model.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured chat client.</returns>
    protected abstract IChatClient CreateClient(TSettings settings, string? modelId);

    /// <inheritdoc />
    IChatClient IAiChatCapability.CreateClient(object? settings, string? modelId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        return CreateClient((TSettings)settings, modelId);
    }
}

/// <summary>
/// Base implementation of an AI embedding capability.
/// </summary>
public abstract class AiEmbeddingCapabilityBase(IAiProvider provider) : AiCapabilityBase(provider), IAiEmbeddingCapability
{
    /// <inheritdoc />
    public override AiCapability Kind => AiCapability.Embedding;

    /// <summary>
    /// Creates an embedding generator with the specified model.
    /// </summary>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured embedding generator.</returns>
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(string? modelId);

    /// <inheritdoc />
    IEmbeddingGenerator<string, Embedding<float>> IAiEmbeddingCapability.CreateGenerator(object? settings, string? modelId)
        => CreateGenerator(modelId);
}

/// <summary>
/// Base implementation of an AI embedding capability with specific settings.
/// </summary>
/// <typeparam name="TSettings">The provider-specific settings type.</typeparam>
public abstract class AiEmbeddingCapabilityBase<TSettings>(IAiProvider provider) : AiCapabilityBase<TSettings>(provider), IAiCapability<TSettings>, IAiEmbeddingCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AiCapability Kind => AiCapability.Embedding;

    /// <summary>
    /// Creates an embedding generator with the provided settings and model.
    /// </summary>
    /// <param name="settings">Provider-specific settings.</param>
    /// <param name="modelId">Optional model ID. If null, use provider's default.</param>
    /// <returns>A configured embedding generator.</returns>
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(TSettings settings, string? modelId);

    /// <inheritdoc />
    IEmbeddingGenerator<string, Embedding<float>> IAiEmbeddingCapability.CreateGenerator(object? settings, string? modelId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        return CreateGenerator((TSettings)settings, modelId);
    }
}
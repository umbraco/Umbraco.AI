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
                "Use IConfiguredProvider from IAiConnectionService.GetConfiguredProviderAsync().");
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
    /// <param name="settings"></param>
    /// <returns></returns>
    IChatClient CreateClient(object? settings = null);
}

/// <summary>
/// Defines an AI capability for generating embeddings.
/// </summary>
public interface IAiEmbeddingCapability : IAiCapability
{   
    /// <summary>
    /// Creates an embedding generator with the provided settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings);
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
    /// Creates a chat client.
    /// </summary>
    /// <returns></returns>
    protected abstract IChatClient CreateClient();
    
    IChatClient IAiChatCapability.CreateClient(object? settings)
        => CreateClient();
}

/// <summary>
/// Base implementation of an AI chat capability with specific settings.
/// </summary>
/// <typeparam name="TSettings"></typeparam>
public abstract class AiChatCapabilityBase<TSettings>(IAiProvider provider) : AiCapabilityBase<TSettings>(provider), IAiCapability<TSettings>, IAiChatCapability
    where TSettings : class
{
    /// <inheritdoc />
    public override AiCapability Kind => AiCapability.Chat;
    
    /// <summary>
    /// Creates a chat client with the provided settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    protected abstract IChatClient CreateClient(TSettings settings);

    /// <inheritdoc />
    IChatClient IAiChatCapability.CreateClient(object? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateClient));
        return CreateClient((TSettings)settings);
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
    /// Creates an embedding generator.
    /// </summary>
    /// <returns></returns>
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateGenerator();
    
    /// <inheritdoc />
    IEmbeddingGenerator<string, Embedding<float>> IAiEmbeddingCapability.CreateGenerator(object? settings)
        => CreateGenerator();
}

/// <summary>
/// Base implementation of an AI embedding capability with specific settings.
/// </summary>
/// <typeparam name="TSettings"></typeparam>
public abstract class AiEmbeddingCapabilityBase<TSettings>(IAiProvider provider) : AiCapabilityBase<TSettings>(provider), IAiCapability<TSettings>, IAiEmbeddingCapability 
    where TSettings : class
{
    /// <inheritdoc />
    public override AiCapability Kind => AiCapability.Embedding;
    
    /// <summary>
    /// Creates an embedding generator with the provided settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <returns></returns>
    protected abstract IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(TSettings settings);

    /// <inheritdoc />
    IEmbeddingGenerator<string, Embedding<float>> IAiEmbeddingCapability.CreateGenerator(object? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CapabilityGuards.ThrowIfUnresolvedSettings(settings, nameof(CreateGenerator));
        return CreateGenerator((TSettings)settings);
    }
}
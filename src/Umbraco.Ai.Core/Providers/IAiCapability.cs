using System.Text.Json;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Providers;

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
public abstract class AiCapabilityBase : IAiCapability
{
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
public abstract class AiCapabilityBase<TSettings> : IAiCapability
{
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
        => GetModelsAsync(ResolveSettings(settings), cancellationToken);
    
    /// <summary>
    /// Resolves the provided settings object to the expected settings type.
    /// </summary>
    /// <param name="settings"></param>
    /// <typeparam name="TSettings"></typeparam>
    /// <returns></returns>
    protected TSettings ResolveSettings(object? settings)
    {
        return settings switch
        {
            TSettings typedSettings => typedSettings,
            JsonElement jsonElement => jsonElement.Deserialize<TSettings>()!,
            _ => default!
        };
    }
}

/// <summary>
/// Base implementation of an AI chat capability.
/// </summary>
public abstract class AiChatCapabilityBase : AiCapabilityBase, IAiChatCapability
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
public abstract class AiChatCapabilityBase<TSettings> : AiCapabilityBase<TSettings>, IAiCapability<TSettings>, IAiChatCapability
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
        => CreateClient(ResolveSettings(settings));
}

/// <summary>
/// Base implementation of an AI embedding capability.
/// </summary>
public abstract class AiEmbeddingCapabilityBase : AiCapabilityBase, IAiEmbeddingCapability
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
public abstract class AiEmbeddingCapabilityBase<TSettings> : AiCapabilityBase<TSettings>, IAiCapability<TSettings>, IAiEmbeddingCapability 
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
        => CreateGenerator(ResolveSettings(settings));
}
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.OpenAi;

/// <summary>
/// Settings for the OpenAI provider.
/// </summary>
public class OpenAiProviderSettings
{
    /// <summary>
    /// The API key for authenticating with OpenAI services.
    /// </summary>
    public string? ApiKey { get; set; }
}

/// <summary>
/// AI provider for OpenAI services.
/// </summary>
[AiProvider("openai", "OpenAI")]
public class OpenAiProvider : AiProviderBase<OpenAiProviderSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public OpenAiProvider(IServiceProvider serviceProvider) 
        : base(serviceProvider)
    {
        WithCapability<OpenAiChatCapability>();
        WithCapability<OpenAiEmbeddingCapability>();
    }
}

/// <summary>
/// AI chat feature for OpenAI provider.
/// </summary>
public class OpenAiChatCapability : AiChatCapabilityBase<OpenAiProviderSettings>
{
    /// <inheritdoc />
    protected override Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(OpenAiProviderSettings settings,  CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(OpenAiProviderSettings settings)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// AI embedding feature for OpenAI provider.
/// </summary>
public class OpenAiEmbeddingCapability : AiEmbeddingCapabilityBase<OpenAiProviderSettings>
{
    /// <inheritdoc />
    protected override Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(OpenAiProviderSettings settings,  CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(OpenAiProviderSettings settings)
    {
        throw new NotImplementedException();
    }
}
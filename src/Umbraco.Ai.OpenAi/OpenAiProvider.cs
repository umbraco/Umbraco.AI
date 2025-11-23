using System.ComponentModel.DataAnnotations;
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
    [AiSetting(
        Label = "API Key",
        Description = "Your OpenAI API key from platform.openai.com",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 1
    )]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional organization ID for OpenAI API requests.
    /// </summary>
    [AiSetting(
        Label = "Organization ID",
        Description = "Optional: Your OpenAI organization ID",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        SortOrder = 2
    )]
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Custom API endpoint URL.
    /// </summary>
    [AiSetting(
        Label = "API Endpoint",
        Description = "Custom API endpoint (leave empty for default)",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        DefaultValue = "https://api.openai.com/v1",
        SortOrder = 3
    )]
    public string? Endpoint { get; set; }
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
public class OpenAiChatCapability(OpenAiProvider provider) : AiChatCapabilityBase<OpenAiProviderSettings>(provider)
{
    /// <inheritdoc />
    protected override Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Return commonly used OpenAI models
        // In the future, this could call the OpenAI API to get the current list
        var models = new List<AiModelDescriptor>
        {
            new(new AiModelRef(Provider.Id, "gpt-4o"), "GPT-4o"),
            new(new AiModelRef(Provider.Id, "gpt-4o-mini"), "GPT-4o Mini"),
            new(new AiModelRef(Provider.Id, "gpt-4-turbo"), "GPT-4 Turbo"),
            new(new AiModelRef(Provider.Id, "gpt-4"), "GPT-4"),
            new(new AiModelRef(Provider.Id, "gpt-3.5-turbo"), "GPT-3.5 Turbo")
        };

        return Task.FromResult<IReadOnlyList<AiModelDescriptor>>(models);
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(OpenAiProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required.");
        }

        return new OpenAI.OpenAIClient(settings.ApiKey)
            .GetChatClient("gpt-4o")
            .AsIChatClient();
    }
}

/// <summary>
/// AI embedding feature for OpenAI provider.
/// </summary>
public class OpenAiEmbeddingCapability(OpenAiProvider provider) : AiEmbeddingCapabilityBase<OpenAiProviderSettings>(provider)
{
    /// <inheritdoc />
    protected override Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Return commonly used OpenAI embedding models
        var models = new List<AiModelDescriptor>
        {
            new(new AiModelRef(Provider.Id, "text-embedding-3-large"), "Text Embedding 3 Large"),
            new(new AiModelRef(Provider.Id, "text-embedding-3-small"), "Text Embedding 3 Small"),
            new(new AiModelRef(Provider.Id, "text-embedding-ada-002"), "Ada 002")
        };

        return Task.FromResult<IReadOnlyList<AiModelDescriptor>>(models);
    }

    /// <inheritdoc />
    protected override IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(OpenAiProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required.");
        }

        return new OpenAI.OpenAIClient(settings.ApiKey)
            .GetEmbeddingClient("text-embedding-3-small")
            .AsIEmbeddingGenerator();
    }
}
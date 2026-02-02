using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// AI chat capability for Microsoft AI Foundry provider.
/// </summary>
/// <remarks>
/// Supports all chat models available through Microsoft AI Foundry, including
/// OpenAI (GPT-4, GPT-4o), Mistral, Llama, Cohere, Phi, and more.
/// </remarks>
public class MicrosoftFoundryChatCapability(MicrosoftFoundryProvider provider) : AIChatCapabilityBase<MicrosoftFoundryProviderSettings>(provider)
{
    private const string DefaultChatModel = "gpt-4o";

    private new MicrosoftFoundryProvider Provider => (MicrosoftFoundryProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                MicrosoftFoundryModelUtilities.FormatDisplayName(m.Id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(MicrosoftFoundryProviderSettings settings, string? modelId)
    {
        var model = modelId ?? DefaultChatModel;
        return MicrosoftFoundryProvider.CreateChatCompletionsClient(settings, model)
            .AsIChatClient(model);
    }

    private static bool IsChatModel(MicrosoftFoundryModelInfo model)
    {
        // If capabilities are provided, use them
        if (model.Capabilities is not null)
        {
            return model.Capabilities.ChatCompletion;
        }

        // Fallback: exclude known embedding model patterns
        var id = model.Id.ToLowerInvariant();
        return !id.Contains("embedding") && !id.Contains("embed");
    }
}

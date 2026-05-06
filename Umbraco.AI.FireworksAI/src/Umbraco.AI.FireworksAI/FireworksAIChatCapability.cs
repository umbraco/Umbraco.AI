using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.FireworksAI;

/// <summary>
/// AI chat capability for Fireworks AI.
/// </summary>
public class FireworksAIChatCapability(FireworksAIProvider provider)
    : AIChatCapabilityBase<FireworksAIProviderSettings>(provider)
{
    /// <summary>
    /// Fallback model id used when a profile doesn't specify one and the catalog
    /// call fails. Chosen for broad availability; any current Fireworks account
    /// has access to a Llama 3.x chat model.
    /// </summary>
    private const string DefaultChatModel = "accounts/fireworks/models/llama-v3p3-70b-instruct";

    private new FireworksAIProvider Provider => (FireworksAIProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        FireworksAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Name),
                FireworksAIModelUtilities.FormatDisplayName(m.Name)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(FireworksAIProviderSettings settings, string? modelId)
    {
        var inner = FireworksAIProvider.CreateOpenAIClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();

        return new FireworksAIStructuredOutputChatClient(inner);
    }

    private static bool IsChatModel(FireworksAIModelInfo model)
        => model.ConversationConfig is not null;
}

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.TogetherAI;

/// <summary>
/// AI chat capability for Together AI provider.
/// Filters models dynamically by Together's declared <c>type</c> field, so no
/// code changes are required when Together adds new chat models.
/// </summary>
public class TogetherAIChatCapability(TogetherAIProvider provider)
    : AIChatCapabilityBase<TogetherAIProviderSettings>(provider)
{
    private const string DefaultChatModel = "meta-llama/Llama-3.3-70B-Instruct-Turbo";
    private const string ChatType = "chat";

    private new TogetherAIProvider Provider => (TogetherAIProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        TogetherAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(m => string.Equals(m.Type, ChatType, StringComparison.OrdinalIgnoreCase))
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                m.DisplayName ?? TogetherAIModelUtilities.FormatDisplayName(m.Id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(TogetherAIProviderSettings settings, string? modelId)
        => TogetherAIProvider.CreateOpenAIClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();
}

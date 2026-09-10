using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.ZAI;

/// <summary>
/// AI chat capability for the Z.AI provider.
/// </summary>
public class ZAIChatCapability(ZAIProvider provider) : AIChatCapabilityBase<ZAIProviderSettings>(provider)
{
    private const string DefaultChatModel = "glm-4.5";

    private new ZAIProvider Provider => (ZAIProvider)base.Provider;

    /// <summary>
    /// Patterns that match Z.AI's GLM chat models. Broad on purpose so new model
    /// families (e.g. glm-6-*) are picked up without code changes.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^glm-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        ZAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                ZAIModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(ZAIProviderSettings settings, string? modelId)
    {
        return ZAIProvider.CreateZAIClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

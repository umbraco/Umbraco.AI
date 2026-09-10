using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Alibaba;

/// <summary>
/// AI chat capability for the Alibaba Cloud (Qwen) provider.
/// </summary>
public class AlibabaChatCapability(AlibabaProvider provider) : AIChatCapabilityBase<AlibabaProviderSettings>(provider)
{
    private const string DefaultChatModel = "qwen-plus";

    private new AlibabaProvider Provider => (AlibabaProvider)base.Provider;

    /// <summary>
    /// Patterns that match Qwen chat models. Broad on purpose so new model families
    /// (e.g. qwen4-*) are picked up without code changes. Model Studio also fronts other
    /// vendors' models (DeepSeek, Kimi, GLM, MiniMax) behind the same endpoint; those stay
    /// out of scope here since dedicated Umbraco.AI providers already cover some of them.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^qwen", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AlibabaProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                AlibabaModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(AlibabaProviderSettings settings, string? modelId)
        => AlibabaProvider.CreateAlibabaClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

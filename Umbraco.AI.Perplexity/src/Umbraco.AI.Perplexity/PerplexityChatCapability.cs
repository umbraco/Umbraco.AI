using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Perplexity;

/// <summary>
/// AI chat capability for the Perplexity provider.
/// </summary>
public class PerplexityChatCapability(PerplexityProvider provider) : AIChatCapabilityBase<PerplexityProviderSettings>(provider)
{
    private const string DefaultChatModel = "sonar";

    private new PerplexityProvider Provider => (PerplexityProvider)base.Provider;

    /// <summary>
    /// Patterns to exclude from the chat model list. Perplexity may expose
    /// non-chat models (e.g., embeddings) under the same ownership; keep these
    /// out of the chat dropdown.
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"^pplx-embed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        PerplexityProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                PerplexityModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(PerplexityProviderSettings settings, string? modelId)
    {
        var inner = PerplexityProvider.CreatePerplexityClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();
        return new PerplexityChatClient(inner);
    }

    private static bool IsChatModel(string modelId)
        => !ExcludePatterns.Any(p => p.IsMatch(modelId));
}

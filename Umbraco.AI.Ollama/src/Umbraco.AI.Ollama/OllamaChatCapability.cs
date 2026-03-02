using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Ollama;

/// <summary>
/// AI chat capability for Ollama provider.
/// </summary>
public class OllamaChatCapability(OllamaProvider provider, ILogger<OllamaChatCapability> logger) : AIChatCapabilityBase<OllamaProviderSettings>(provider)
{
    private const string DefaultChatModel = "llama3.2";

    private new OllamaProvider Provider => (OllamaProvider)base.Provider;

    /// <summary>
    /// Patterns that exclude embedding-only models.
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"embed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OllamaProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

            return allModels
                .Where(IsChatModel)
                .Select(id => new AIModelDescriptor(
                    new AIModelRef(Provider.Id, id),
                    OllamaModelUtilities.FormatDisplayName(id)))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve available chat models from Ollama. Returning empty list to prevent capability failure.");
            return Array.Empty<AIModelDescriptor>();
        }
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(OllamaProviderSettings settings, string? modelId)
    {
        var client = Provider.CreateOllamaClient(settings);
        client.SelectedModel = modelId ?? DefaultChatModel;
        return client;
    }

    private static bool IsChatModel(string modelId)
        => !ExcludePatterns.Any(p => p.IsMatch(modelId));
}

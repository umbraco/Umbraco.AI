using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Gemini;

/// <summary>
/// AI chat capability for Google Gemini provider.
/// </summary>
public class GeminiChatCapability(GeminiProvider provider) : AiChatCapabilityBase<GeminiProviderSettings>(provider)
{
    private const string DefaultChatModel = "gemini-2.0-flash";

    private new GeminiProvider Provider => (GeminiProvider)base.Provider;

    /// <summary>
    /// Known Gemini models that support chat.
    /// </summary>
    private static readonly string[] KnownChatModels =
    [
        "gemini-2.0-flash",
        "gemini-2.0-flash-lite",
        "gemini-1.5-pro",
        "gemini-1.5-flash",
        "gemini-1.5-flash-8b",
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        GeminiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Try to get models from API, fall back to known models if API call fails
        try
        {
            var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken).ConfigureAwait(false);

            // Filter to only include known chat models that are available from the API
            var availableModels = allModels
                .Where(id => KnownChatModels.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (availableModels.Count > 0)
            {
                return availableModels
                    .Select(id => new AiModelDescriptor(
                        new AiModelRef(Provider.Id, id),
                        GeminiModelUtilities.FormatDisplayName(id)))
                    .ToList();
            }
        }
        catch
        {
            // Fall through to return known models
        }

        // Return hardcoded list of known chat models as fallback
        return KnownChatModels
            .Select(id => new AiModelDescriptor(
                new AiModelRef(Provider.Id, id),
                GeminiModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(GeminiProviderSettings settings, string? modelId)
        => GeminiProvider.CreateGeminiClient(settings)
            .AsIChatClient(modelId ?? DefaultChatModel);
}

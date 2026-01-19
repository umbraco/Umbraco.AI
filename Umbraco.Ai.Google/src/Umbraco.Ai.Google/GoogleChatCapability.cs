using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Google;

/// <summary>
/// AI chat capability for Google provider.
/// </summary>
public class GoogleChatCapability(GoogleProvider provider) : AiChatCapabilityBase<GoogleProviderSettings>(provider)
{
    private const string DefaultChatModel = "gemini-2.0-flash";

    private new GoogleProvider Provider => (GoogleProvider)base.Provider;

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
        GoogleProviderSettings settings,
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
                        GoogleModelUtilities.FormatDisplayName(id)))
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
                GoogleModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(GoogleProviderSettings settings, string? modelId)
        => GoogleProvider.CreateGoogleClient(settings)
            .AsIChatClient(modelId ?? DefaultChatModel);
}

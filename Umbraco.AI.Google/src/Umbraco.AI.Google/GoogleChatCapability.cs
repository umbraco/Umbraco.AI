using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Google;

/// <summary>
/// AI chat capability for Google provider.
/// </summary>
public class GoogleChatCapability(GoogleProvider provider) : AIChatCapabilityBase<GoogleProviderSettings>(provider)
{
    private const string DefaultChatModel = "gemini-3-flash-preview";

    private new GoogleProvider Provider => (GoogleProvider)base.Provider;

    /// <summary>
    /// Known Gemini models that support chat.
    /// </summary>
    private static readonly string[] KnownChatModels =
    [
        "gemini-3-flash-preview"
        "gemini-2.0-flash",
        "gemini-2.0-flash-lite",
        "gemini-1.5-pro",
        "gemini-1.5-flash",
        "gemini-1.5-flash-8b",
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        GoogleProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        // Try to get models from API, fall back to known models if API call fails
        try
        {
            var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

            // Filter to only include known chat models that are available from the API
            var availableModels = allModels
                .Where(id => KnownChatModels.Contains(id, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (availableModels.Count > 0)
            {
                return availableModels
                    .Select(id => new AIModelDescriptor(
                        new AIModelRef(Provider.Id, id),
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
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                GoogleModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(GoogleProviderSettings settings, string? modelId)
        => GoogleProvider.CreateGoogleClient(settings)
            .AsIChatClient(modelId ?? DefaultChatModel);
}

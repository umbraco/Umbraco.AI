using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Google;

/// <summary>
/// AI chat capability for Google provider.
/// </summary>
public partial class GoogleChatCapability(GoogleProvider provider) : AIChatCapabilityBase<GoogleProviderSettings>(provider)
{
    private new GoogleProvider Provider => (GoogleProvider)base.Provider;

    /// <summary>
    /// Pattern that matches Gemini chat models (flash, pro variants).
    /// </summary>
    [GeneratedRegex(@"^gemini-.*\b(flash|pro)\b", RegexOptions.IgnoreCase)]
    private static partial Regex IncludePattern();

    /// <summary>
    /// Pattern that excludes non-chat variants (image generation, TTS, audio, computer-use).
    /// </summary>
    [GeneratedRegex(@"image|tts|audio|computer-use", RegexOptions.IgnoreCase)]
    private static partial Regex ExcludePattern();

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        GoogleProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                GoogleModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override async Task<IChatClient> CreateClientAsync(
        GoogleProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        if (modelId is null)
        {
            modelId = await ResolveDefaultModelAsync(settings, cancellationToken);
        }

        if (modelId is null)
        {
            throw new InvalidOperationException(
                "No Google chat models are available. " +
                "Check API credentials, network connectivity, and that the Google API returns available models.");
        }

        return GoogleProvider.CreateGoogleClient(settings).AsIChatClient(modelId);
    }

    /// <summary>
    /// Resolves the default chat model by querying the API for the latest flash model.
    /// </summary>
    private async Task<string?> ResolveDefaultModelAsync(
        GoogleProviderSettings settings,
        CancellationToken cancellationToken)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        // Prefer stable flash models, then fall back to any chat-capable gemini model
        return allModels
            .Where(IsChatModel)
            .OrderByDescending(id => id.Contains("flash", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(id => !id.Contains("preview", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(id => id)
            .FirstOrDefault();
    }

    private static bool IsChatModel(string modelId)
        => IncludePattern().IsMatch(modelId)
           && !ExcludePattern().IsMatch(modelId);
}

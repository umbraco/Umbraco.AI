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
    private const string ErrorModelId = "error";
    private string ErrorMessage = "";
    private new GoogleProvider Provider => (GoogleProvider)base.Provider;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        GoogleProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);
            if (allModels == null)
            {
                return CreateErrorModelDescriptors("No models were returned by the Google API.");
            }

            // Filter to only include known chat models that are available from the API
            var availableModels = allModels
                .Where(id => id.Contains("gemini", StringComparison.OrdinalIgnoreCase)
                             && (id.Contains("flash", StringComparison.OrdinalIgnoreCase)
                                 || id.Contains("preview", StringComparison.OrdinalIgnoreCase)))
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
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to retrieve models from Google API: {ex.Message}";
            return CreateErrorModelDescriptors(ErrorMessage);
        }

        ErrorMessage = "No compatible Google chat models were found.";
        return CreateErrorModelDescriptors(ErrorMessage);
    }

    /// <summary>
    /// Helper to create a list with a single error AIModelDescriptor.
    /// </summary>
    /// <param name="message">The error message to include.</param>
    /// <returns>A list containing one error AIModelDescriptor.</returns>
    private static List<AIModelDescriptor> CreateErrorModelDescriptors(string message)
    {
        return new List<AIModelDescriptor>
        {
            new AIModelDescriptor(
                new AIModelRef(ErrorModelId, ErrorModelId),
                message,
                new Dictionary<string, string> { { "error", message } })
        };
    }


    /// <inheritdoc />
    protected override IChatClient CreateClient(GoogleProviderSettings settings, string? modelId)
        => GoogleProvider.CreateGoogleClient(settings)
            .AsIChatClient(modelId ?? DefaultChatModel);
}

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// AI speech-to-text capability for OpenAI provider.
/// </summary>
public class OpenAISpeechToTextCapability(OpenAIProvider provider) : AISpeechToTextCapabilityBase<OpenAIProviderSettings>(provider)
{
    private const string DefaultSpeechToTextModel = "gpt-4o-transcribe";

    private new OpenAIProvider Provider => (OpenAIProvider)base.Provider;

    /// <summary>
    /// Patterns that match speech-to-text models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^whisper-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4o-transcribe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4o-mini-transcribe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsSpeechToTextModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                OpenAIModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override ISpeechToTextClient CreateClient(OpenAIProviderSettings settings, string? modelId)
        => OpenAIProvider.CreateOpenAIClient(settings)
            .GetAudioClient(modelId ?? DefaultSpeechToTextModel)
            .AsISpeechToTextClient();

    private static bool IsSpeechToTextModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}

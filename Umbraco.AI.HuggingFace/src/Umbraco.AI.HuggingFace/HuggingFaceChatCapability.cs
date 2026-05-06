using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.HuggingFace;

/// <summary>
/// AI chat capability for the Hugging Face provider.
/// </summary>
public class HuggingFaceChatCapability(HuggingFaceProvider provider) : AIChatCapabilityBase<HuggingFaceProviderSettings>(provider)
{
    private const string DefaultChatModel = "openai/gpt-oss-120b";

    private new HuggingFaceProvider Provider => (HuggingFaceProvider)base.Provider;

    // HF model IDs are formatted "vendor/model-name", optionally with a ":provider" or
    // ":fastest|cheapest|preferred" routing suffix. The router's /v1/models endpoint
    // returns chat-capable models; the slash check is enough to drop anything malformed.
    private static readonly Regex IncludePattern =
        new(@"^[A-Za-z0-9._-]+/[A-Za-z0-9._:-]+$", RegexOptions.Compiled);

    // Drop obvious non-chat artefacts in case the router ever lists them.
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"(?:^|/)(flux|sdxl|sd-|stable-diffusion)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"-(embed|embedding|reranker|tts|whisper|asr)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        HuggingFaceProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                HuggingFaceModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(HuggingFaceProviderSettings settings, string? modelId)
        => HuggingFaceProvider.CreateOpenAIClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();

    private static bool IsChatModel(string modelId)
        => IncludePattern.IsMatch(modelId)
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}

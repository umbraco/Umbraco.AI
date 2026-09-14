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

    /// <summary>
    /// Patterns that exclude non-chat models even when they match an include pattern.
    /// Alibaba reuses the "qwen" prefix for other capabilities — image generation
    /// (qwen-image-*), speech (qwen-audio-*-asr-*, qwen3-asr-*, qwen3-tts-*), machine
    /// translation (qwen-mt-*), live translation (qwen3-livetranslate-*), speech-to-speech
    /// (qwen3-s2s-*), realtime WebSocket variants (*-realtime), and one text embedding
    /// model (qwen3.7-text-embedding) — none of these speak the chat/completions shape.
    /// Confirmed against a live model list (165 entries) during development.
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"-image-|-audio-|-asr-|-tts-|-mt-|-livetranslate-|-s2s-|-realtime|embedding", RegexOptions.IgnoreCase | RegexOptions.Compiled),
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
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}

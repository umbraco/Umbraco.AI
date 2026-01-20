using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.OpenAi;

/// <summary>
/// AI chat capability for OpenAI provider.
/// </summary>
public class OpenAiChatCapability(OpenAiProvider provider) : AiChatCapabilityBase<OpenAiProviderSettings>(provider)
{
    private const string DefaultChatModel = "gpt-4o";
    
    private new OpenAiProvider Provider => (OpenAiProvider)base.Provider;

    /// <summary>
    /// Patterns that match chat/completion models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^gpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o1", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^chatgpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Patterns to exclude from chat models (e.g., realtime, audio-only variants).
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"-realtime", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4o-transcribe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4o-mini-transcribe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(
        OpenAiProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AiModelDescriptor(
                new AiModelRef(Provider.Id, id),
                OpenAiModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(OpenAiProviderSettings settings, string? modelId)
        => OpenAiProvider.CreateOpenAiClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}
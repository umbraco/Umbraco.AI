using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Mistral;

/// <summary>
/// AI chat capability for Mistral provider.
/// </summary>
public class MistralChatCapability(MistralProvider provider) : AIChatCapabilityBase<MistralProviderSettings>(provider)
{
    private const string DefaultChatModel = "mistral-large-latest";

    private new MistralProvider Provider => (MistralProvider)base.Provider;

    /// <summary>
    /// Patterns that match Mistral chat models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^(mistral|open-mistral|open-mixtral|codestral|pixtral|ministral|magistral)-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Patterns that exclude non-chat models even when they match an include pattern.
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"embed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"moderation", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"ocr", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MistralProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                MistralModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(MistralProviderSettings settings, string? modelId)
    {
        var effectiveModelId = modelId ?? DefaultChatModel;
        var completions = MistralProvider.CreateMistralClient(settings).Completions;

        return new ChatClientBuilder(completions)
            .ConfigureOptions(options => options.ModelId ??= effectiveModelId)
            .Build();
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}

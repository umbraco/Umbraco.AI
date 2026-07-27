using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// AI chat capability for Anthropic provider.
/// </summary>
/// <remarks>
/// The <paramref name="logger"/> is optional so the capability can still be constructed directly (in tests,
/// or by a caller that predates it) without a DI container supplying one.
/// </remarks>
public class AnthropicChatCapability(
    AnthropicProvider provider,
    ILogger<AnthropicChatCapability>? logger = null)
    : AIChatCapabilityBase<AnthropicProviderSettings>(provider)
{
    private const string DefaultChatModel = "claude-sonnet-4-20250514";
    
    private new AnthropicProvider Provider => (AnthropicProvider)base.Provider;

    /// <summary>
    /// Patterns that match Claude chat models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^claude-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AnthropicProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                AnthropicModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(AnthropicProviderSettings settings, string? modelId)
    {
        var inner = AnthropicProvider.CreateAnthropicClient(settings)
            .Beta.AsIChatClient(modelId);

        // Wrapped innermost, so the sampling parameters are filtered against the target model no matter
        // which caller assembled the ChatOptions. See AnthropicSamplingParameterChatClient.
        return new AnthropicSamplingParameterChatClient(inner, modelId, logger);
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// AI chat capability for Anthropic provider.
/// </summary>
public class AnthropicChatCapability(AnthropicProvider provider)
    : AIChatCapabilityBase<AnthropicProviderSettings, AnthropicChatCapabilitySettings>(provider)
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
    /// <remarks>
    /// Claude 4.7 and later reject an explicit thinking budget, so it is declared per model — otherwise
    /// the profile editor would offer a setting whose only effect is a 400 on every current frontier
    /// model. The set that accepts a budget is the closed one (4.6 and earlier), so the predicate is
    /// written as an allow-list and unknown models read as unsupported.
    /// </remarks>
    public override AIModelSettingSupport GetSettingSupport(string modelId)
        => AnthropicModelUtilities.SupportsThinkingBudget(modelId)
            ? AIModelSettingSupport.Default
            : new AIModelSettingSupport
            {
                UnsupportedCapabilitySettings = [nameof(AnthropicChatCapabilitySettings.ThinkingBudgetTokens)],
            };

    /// <inheritdoc />
    protected override IChatClient CreateClient(AnthropicProviderSettings settings, string? modelId)
        => AnthropicProvider.CreateAnthropicClient(settings)
            .Beta.AsIChatClient(modelId);

    /// <inheritdoc />
    /// <remarks>
    /// Enables Claude's extended thinking with the configured token budget. The Anthropic
    /// (tryAGI) Microsoft.Extensions.AI adapter reads the <c>"thinking"</c> entry from
    /// <see cref="ChatOptions.AdditionalProperties"/> when building the request.
    /// Skipped on models that reject a budget — the same predicate that drives
    /// <see cref="GetSettingSupport"/> — so a profile carrying a stale value cannot fail the request.
    /// </remarks>
    protected override void ApplyCapabilitySettings(
        AnthropicChatCapabilitySettings capabilitySettings,
        string? modelId,
        ChatOptions options)
    {
        if (capabilitySettings.ThinkingBudgetTokens is not { } budgetTokens || budgetTokens <= 0)
        {
            return;
        }

        if (!AnthropicModelUtilities.SupportsThinkingBudget(modelId ?? DefaultChatModel))
        {
            return;
        }

        (options.AdditionalProperties ??= new AdditionalPropertiesDictionary())["thinking"] = budgetTokens;
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
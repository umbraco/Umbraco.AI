using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

#pragma warning disable OPENAI001 // OpenAI Responses API (reasoning options) is experimental in the SDK

namespace Umbraco.AI.OpenAI;

/// <summary>
/// AI chat capability for OpenAI provider.
/// </summary>
public class OpenAIChatCapability(OpenAIProvider provider)
    : AIChatCapabilityBase<OpenAIProviderSettings, OpenAIChatCapabilitySettings>(provider)
{
    private const string DefaultChatModel = "gpt-4o";

    private new OpenAIProvider Provider => (OpenAIProvider)base.Provider;

    /// <summary>
    /// Patterns that match chat/completion models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^gpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o1", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o4", RegexOptions.IgnoreCase | RegexOptions.Compiled),
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
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                OpenAIModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reasoning effort applies to the o-series and GPT-5 only, so it is declared per model rather than
    /// per provider — otherwise the profile editor would offer it on a gpt-4o profile. The stable set
    /// here is the list of reasoning families, so the predicate is written as an allow-list and inverted
    /// into the declaration.
    /// </remarks>
    public override AIModelSettingSupport GetSettingSupport(string modelId)
        => OpenAIModelUtilities.SupportsReasoningEffort(modelId)
            ? AIModelSettingSupport.Default
            : new AIModelSettingSupport
            {
                UnsupportedCapabilitySettings = [nameof(OpenAIChatCapabilitySettings.ReasoningEffort)],
            };

    /// <inheritdoc />
    [Experimental("OPENAI001")]
    protected override IChatClient CreateClient(OpenAIProviderSettings settings, string? modelId)
        => OpenAIProvider.CreateOpenAIClient(settings)
            .GetResponsesClient()
            .AsIChatClient(modelId ?? DefaultChatModel);

    /// <inheritdoc />
    /// <remarks>
    /// Translates the profile's reasoning-effort setting into the Responses API's
    /// <see cref="ResponseReasoningOptions.ReasoningEffortLevel"/> via
    /// <see cref="ChatOptions.RawRepresentationFactory"/>. Any existing factory is preserved.
    /// Skipped entirely on models that do not accept a reasoning effort — the same predicate that drives
    /// <see cref="GetSettingSupport"/> — so a profile carrying a stale value cannot fail the request.
    /// </remarks>
    protected override void ApplyCapabilitySettings(
        OpenAIChatCapabilitySettings capabilitySettings,
        string? modelId,
        ChatOptions options)
    {
        if (string.IsNullOrWhiteSpace(capabilitySettings.ReasoningEffort))
        {
            return;
        }

        if (!OpenAIModelUtilities.SupportsReasoningEffort(modelId ?? DefaultChatModel))
        {
            return;
        }

        ResponseReasoningEffortLevel? effort = capabilitySettings.ReasoningEffort.Trim().ToLowerInvariant() switch
        {
            "none" => ResponseReasoningEffortLevel.None,
            "minimal" => ResponseReasoningEffortLevel.Minimal,
            "low" => ResponseReasoningEffortLevel.Low,
            "medium" => ResponseReasoningEffortLevel.Medium,
            "high" => ResponseReasoningEffortLevel.High,
            // Unrecognised values (including xhigh/max, which the pinned SDK cannot express) are skipped
            // rather than sent, so a stored value can never fail the request.
            _ => null
        };

        if (effort is null)
        {
            return;
        }

        var previousFactory = options.RawRepresentationFactory;
        options.RawRepresentationFactory = client =>
        {
            var raw = previousFactory?.Invoke(client) as CreateResponseOptions ?? new CreateResponseOptions();
            raw.ReasoningOptions ??= new ResponseReasoningOptions();
            raw.ReasoningOptions.ReasoningEffortLevel = effort;
            return raw;
        };
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}

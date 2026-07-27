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
    /// </remarks>
    protected override void ApplyCapabilitySettings(OpenAIChatCapabilitySettings capabilitySettings, ChatOptions options)
    {
        if (string.IsNullOrWhiteSpace(capabilitySettings.ReasoningEffort))
        {
            return;
        }

        ResponseReasoningEffortLevel? effort = capabilitySettings.ReasoningEffort.Trim().ToLowerInvariant() switch
        {
            "low" => ResponseReasoningEffortLevel.Low,
            "medium" => ResponseReasoningEffortLevel.Medium,
            "high" => ResponseReasoningEffortLevel.High,
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

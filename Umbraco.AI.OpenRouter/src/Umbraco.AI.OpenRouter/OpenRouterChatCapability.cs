using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenRouter;

/// <summary>
/// AI chat capability for the OpenRouter provider.
/// </summary>
public class OpenRouterChatCapability(OpenRouterProvider provider, ILogger<OpenRouterChatCapability> logger)
    : AIChatCapabilityBase<OpenRouterProviderSettings>(provider)
{
    private const string DefaultChatModel = "openai/gpt-4o-mini";

    private new OpenRouterProvider Provider => (OpenRouterProvider)base.Provider;

    /// <summary>
    /// The core sampling settings, paired with the OpenRouter <c>supported_parameters</c> entry that
    /// reports whether a given model accepts them.
    /// </summary>
    private static readonly (string Parameter, string Key)[] SamplingParameters =
    [
        ("temperature", AIProfileSettingKeys.Temperature),
        ("top_p", AIProfileSettingKeys.TopP),
        ("top_k", AIProfileSettingKeys.TopK),
        ("frequency_penalty", AIProfileSettingKeys.FrequencyPenalty),
        ("presence_penalty", AIProfileSettingKeys.PresencePenalty),
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Declarations are attached here from the same logic <see cref="GetSettingsSupport"/> uses, with
    /// each model's listing entry passed in directly rather than read back from the provider's cache.
    /// </remarks>
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenRouterProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                OpenRouterModelUtilities.FormatDisplayName(m.Id, m.Name),
                BuildMetadata(m)))
            .ToList();
    }

    /// <summary>
    /// Combines the settings declaration with the model facts (context window, price) from a listing
    /// entry into the descriptor's metadata. Facts OpenRouter does not report are simply left out.
    /// </summary>
    private static IReadOnlyDictionary<string, string> BuildMetadata(OpenRouterModelInfo info)
    {
        var metadata = new Dictionary<string, string>(BuildSettingsSupport(info).ToMetadata());

        if (TryParseContextLength(info.ContextLength) is { } contextLength)
        {
            Merge(metadata, AIModelMetadata.ForContextWindow(contextLength));
        }

        if (TryGetPricing(info.Pricing) is { } pricing)
        {
            Merge(metadata, AIModelMetadata.ForPricing(pricing));
        }

        return metadata;
    }

    /// <summary>
    /// Adds <paramref name="source"/> to <paramref name="target"/>; existing entries win, matching
    /// core's <c>WithMetadata</c>.
    /// </summary>
    private static void Merge(Dictionary<string, string> target, IReadOnlyDictionary<string, string> source)
    {
        foreach (var (key, value) in source)
        {
            target.TryAdd(key, value);
        }
    }

    /// <summary>
    /// Reads <c>context_length</c> leniently: a positive 32-bit integer, given as a JSON number or a
    /// quoted integer. Anything else (fractional, oversized, wrong type) yields <c>null</c>; this is
    /// a display-only fact and must never fail the listing.
    /// </summary>
    private static int? TryParseContextLength(JsonElement? value)
    {
        var parsed = 0;
        var ok = value switch
        {
            { ValueKind: JsonValueKind.Number } number => number.TryGetInt32(out parsed),
            { ValueKind: JsonValueKind.String } text => int.TryParse(
                text.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out parsed),
            _ => false,
        };

        return ok && parsed > 0 ? parsed : null;
    }

    /// <summary>
    /// Converts OpenRouter's per-token USD strings to a per-million-token price. Returns <c>null</c>
    /// when either value is missing, unparseable or negative (<c>"-1"</c> marks routers with no fixed
    /// price), or when both are zero (free models) — there is no real price to show in those cases.
    /// </summary>
    private static AIModelPricing? TryGetPricing(OpenRouterModelPricing? pricing)
    {
        if (!TryParsePerToken(pricing?.Prompt, out var input)
            || !TryParsePerToken(pricing?.Completion, out var output)
            || (input == 0 && output == 0))
        {
            return null;
        }

        const decimal tokensPerMillion = 1_000_000m;
        return new AIModelPricing(input * tokensPerMillion, output * tokensPerMillion, "USD");
    }

    /// <summary>
    /// Upper sanity bound for a per-token USD price; anything higher is treated as bad data so the
    /// per-million scaling can never overflow.
    /// </summary>
    private const decimal MaxPerTokenPrice = 1_000_000m;

    /// <summary>
    /// Parses a per-token price given as a JSON string or number. Rejects negatives and values above
    /// <see cref="MaxPerTokenPrice"/>; never throws.
    /// </summary>
    private static bool TryParsePerToken(JsonElement? value, out decimal perToken)
    {
        var text = value switch
        {
            { ValueKind: JsonValueKind.String } s => s.GetString(),
            { ValueKind: JsonValueKind.Number } n => n.GetRawText(),
            _ => null,
        };

        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out perToken)
            && perToken is >= 0 and <= MaxPerTokenPrice;
    }

    /// <inheritdoc />
    /// <remarks>
    /// OpenRouter reports, per model, exactly which request parameters it accepts — there is no need
    /// to infer sampling support from vendor-family regexes the way the single-vendor providers do.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => BuildSettingsSupport(Provider.TryGetModelInfo(modelId));

    /// <summary>
    /// Turns a model's listing entry into the settings declaration the editor reads and the base
    /// enforces.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> entry — the model is absent from the last listing, or the listing call failed —
    /// is treated as fully supported. Unlike the single-vendor providers, there is no vendor-family
    /// fallback to reason from: OpenRouter's own catalog is the only source of truth for what a given
    /// model accepts, so "unknown" degrades to the same behaviour as before this declaration existed.
    /// </remarks>
    private static AIModelSettingsSupport BuildSettingsSupport(OpenRouterModelInfo? info)
    {
        if (info?.SupportedParameters is not { Count: > 0 } supported)
        {
            return AIModelSettingsSupport.Default;
        }

        var unsupported = SamplingParameters
            .Where(p => !supported.Contains(p.Parameter))
            .Select(p => p.Key)
            .ToList();

        return unsupported.Count == 0
            ? AIModelSettingsSupport.Default
            : new AIModelSettingsSupport { UnsupportedProfileSettings = unsupported };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Fetches the model list (cached) before building the client, so <see cref="GetSettingsSupport"/>
    /// can read the target model's declared parameters synchronously when the base enforces the
    /// declaration. A failure to list models is not fatal — degrades to treating the model as fully
    /// supported, and logs, since that fallback is less accurate and otherwise invisible.
    /// </remarks>
    protected override async Task<IChatClient> CreateClientAsync(
        OpenRouterProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Provider.GetAvailableModelsAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(
                ex,
                "Could not list OpenRouter models while creating a chat client. Settings support will "
                + "default to fully supported instead of reading the model's declared parameters.");
        }

        var model = modelId ?? DefaultChatModel;

        // The declaration from GetSettingsSupport is enforced by the base, which wraps this client so
        // an unsupported sampling parameter is stripped for a model that rejects it.
        return OpenRouterProvider.CreateOpenAIClient(settings)
            .GetChatClient(model)
            .AsIChatClient();
    }
}

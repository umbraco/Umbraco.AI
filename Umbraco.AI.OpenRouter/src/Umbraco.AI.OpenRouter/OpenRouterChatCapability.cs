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
                BuildSettingsSupport(m).ToMetadata()))
            .ToList();
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

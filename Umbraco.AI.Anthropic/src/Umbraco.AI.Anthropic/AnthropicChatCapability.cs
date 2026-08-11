using System.Text.RegularExpressions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Anthropic;

/// <summary>
/// AI chat capability for Anthropic provider.
/// </summary>
public class AnthropicChatCapability(
    AnthropicProvider provider,
    ILogger<AnthropicChatCapability>? logger)
    : AIChatCapabilityBase<AnthropicProviderSettings, AnthropicChatCapabilitySettings>(provider)
{
    /// <summary>
    /// Initializes a new instance without a logger.
    /// </summary>
    /// <remarks>
    /// Retained so adding the logger parameter stays binary compatible. An optional parameter would not
    /// achieve that — the compiler emits a single constructor and bakes the default in at each call site,
    /// so assemblies compiled against the previous signature would fail to bind.
    /// <para>
    /// The logger is resolved through the service locator, following the same approach as
    /// <c>AIGuardrailEvaluatorBase</c>, so a consumer still on this signature gets real logging rather than
    /// none. Null-conditional because the locator is unset before startup and in unit tests, and logging is
    /// optional here — unlike the required services that pattern usually resolves.
    /// </para>
    /// </remarks>
    [Obsolete("Use the constructor that accepts a logger. Will be removed in v20.")]
    public AnthropicChatCapability(AnthropicProvider provider)
        : this(provider, StaticServiceProvider.Instance?.GetService<ILogger<AnthropicChatCapability>>())
    {
    }

    private const string DefaultChatModel = "claude-sonnet-4-20250514";

    /// <summary>
    /// The max-tokens value the SDK's Microsoft.Extensions.AI adapter sends when the caller sets none.
    /// Mirrored here because a raw representation must supply the value itself.
    /// </summary>
    private const int AdapterDefaultMaxTokens = 1024;
    
    private new AnthropicProvider Provider => (AnthropicProvider)base.Provider;

    /// <summary>
    /// Patterns that match Claude chat models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^claude-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    /// <remarks>
    /// Declarations come from the same builder <see cref="GetSettingsSupport"/> uses, but with the API's
    /// reported <c>capabilities.effort.supported</c> for each listed model passed in directly, rather than
    /// read back from the provider's cache. Only models the API says nothing about fall back to inferring
    /// support from the ID.
    /// </remarks>
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AnthropicProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(m => IsChatModel(m.Id))
            .OrderBy(m => m.Id)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                AnthropicModelUtilities.FormatDisplayName(m.Id),
                BuildSettingsSupport(m.Id, m.SupportsEffort).ToMetadata()))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Also the request-time gate: the base enforces whatever this returns, so the model list, the editor
    /// and the wire all read the same declaration. Effort support prefers what the API reported for the
    /// model (cached from the last listing) and falls back to the ID predicate when it is unknown, which is
    /// the same order <see cref="ApplyCapabilitySettings"/> uses.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => BuildSettingsSupport(
            modelId,
            Provider.TryGetModelCapability(modelId)?.SupportsEffort);

    /// <summary>
    /// Turns a model's reported capabilities into the settings declaration the editor reads and the base
    /// enforces, falling back to the ID-based predicate when the API reported nothing for the model.
    /// </summary>
    /// <remarks>
    /// The sampling half has no equivalent in the API's response, so it comes from the ID predicate — one
    /// predicate behind both the dropped parameter and the editor's account of it.
    /// </remarks>
    private static AIModelSettingsSupport BuildSettingsSupport(string modelId, bool? reportedEffortSupport)
    {
        var supportsEffort = reportedEffortSupport ?? AnthropicModelUtilities.SupportsEffort(modelId);
        var supportsSampling = AnthropicModelUtilities.SupportsSamplingParameters(modelId);

        if (supportsEffort && supportsSampling)
        {
            return AIModelSettingsSupport.Default;
        }

        return new AIModelSettingsSupport
        {
            UnsupportedCapabilitySettings = supportsEffort
                ? []
                : [nameof(AnthropicChatCapabilitySettings.Effort)],
            UnsupportedProfileSettings = supportsSampling
                ? []
                : AIProfileSettingKeys.Sampling,
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Fetches the model list (cached) before building the client, so the per-request settings hook can
    /// read the target model's reported capabilities synchronously. This is the capability's only creation
    /// method: the synchronous <c>CreateClient</c> hook is left unimplemented because every path that
    /// reaches it would skip the prefetch, and both interface entry points route through here anyway.
    /// </para>
    /// <para>
    /// A failure is not fatal — capability data refines the decision but is not required to make a request,
    /// and losing the model list must not stop chat from working — so it degrades to inferring support from
    /// the model ID, and logs, because that fallback is less accurate and otherwise invisible. Cancellation
    /// is not caught: if the caller has given up, this should too rather than continue building a client.
    /// </para>
    /// </remarks>
    protected override async Task<IChatClient> CreateClientAsync(
        AnthropicProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Provider.GetAvailableModelsAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogDebug(
                ex,
                "Could not list Anthropic models while creating a chat client. Per-model capability data "
                + "is unavailable, so setting support will be inferred from the model ID instead.");
        }

        // The declaration from GetSettingsSupport is enforced by the base, which wraps this client so the
        // sampling parameters are stripped for a model that rejects them. See DeclaredSettingsChatClient.
        //
        // Cache-read tokens need no wrapper here: the SDK's adapter already reports them on
        // UsageDetails.CachedInputTokenCount, which is what core reads. See
        // AnthropicCachedTokenReportingTests.
        return Provider.CreateSdkClient(settings)
            .Beta.AsIChatClient(modelId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Sets <c>output_config.effort</c> and the top-level <c>cache_control</c> through
    /// <see cref="ChatOptions.RawRepresentationFactory"/>, the only channel the SDK's
    /// Microsoft.Extensions.AI adapter reads — values placed in
    /// <see cref="ChatOptions.AdditionalProperties"/> are dropped before the request is built.
    /// </para>
    /// <para>
    /// The adapter treats the factory's result as the request base and overwrites only the messages, so
    /// the model and max-tokens values have to be carried into it here or the request would be sent with
    /// whatever this method put there. <c>AnthropicEffortWireTests</c> pins both behaviours down.
    /// </para>
    /// <para>
    /// Both settings contribute to a single composed representation, and each is applied only when the
    /// profile actually carries it, so enabling one never sends the other's field. The per-setting
    /// applicability rules live in <see cref="ResolveEffort"/> and <see cref="ResolveCacheControl"/>.
    /// </para>
    /// <para>
    /// Caching deliberately marks the request's last cacheable block (what the top-level field does) rather
    /// than the end of the system prompt and tool definitions. A block-level marker is not reachable from
    /// here: the adapter <em>appends</em> the caller's instructions after any <c>System</c> blocks this
    /// representation supplies, so a marker placed on one of them would sit ahead of the content worth
    /// caching. <c>AnthropicPromptCachingWireTests</c> pins that down.
    /// </para>
    /// </remarks>
    protected override void ApplyCapabilitySettings(
        AnthropicChatCapabilitySettings capabilitySettings,
        string? modelId,
        ChatOptions options)
    {
        var model = modelId ?? DefaultChatModel;
        var effort = ResolveEffort(capabilitySettings.Effort, model);
        var cacheControl = ResolveCacheControl(capabilitySettings.PromptCaching);

        if (effort is null && cacheControl is null)
        {
            return;
        }

        var previousFactory = options.RawRepresentationFactory;
        var maxTokens = options.MaxOutputTokens ?? AdapterDefaultMaxTokens;

        options.RawRepresentationFactory = client =>
        {
            var existing = previousFactory?.Invoke(client) as MessageCreateParams;

            var request = existing ?? new MessageCreateParams
            {
                Model = model,
                MaxTokens = maxTokens,
                Messages = [],
            };

            if (effort is not null)
            {
                // Copy an existing output config rather than rebuilding it: assigning a sibling property
                // marks it present in the payload even when the value is null, and the API rejects the ones
                // it does not expect ("output_config.task_budget: Extra inputs are not permitted"). Only
                // effort is set. For the same reason each setting is applied only when it has a value,
                // rather than assigned unconditionally from a nullable local.
                request = request with
                {
                    OutputConfig = request.OutputConfig is { } priorOutputConfig
                        ? priorOutputConfig with { Effort = effort }
                        : new BetaOutputConfig { Effort = effort },
                };
            }

            if (cacheControl is not null)
            {
                request = request with { CacheControl = cacheControl };
            }

            return request;
        };
    }

    /// <summary>
    /// Translates the stored effort level into the SDK's enum, or <c>null</c> when it is unset or the model
    /// will not accept it.
    /// </summary>
    /// <remarks>
    /// Skipped when the model does not accept effort at all, or does not accept the configured level — the
    /// same predicates that drive <see cref="GetSettingsSupport"/> — so a profile carrying a value the model
    /// rejects cannot fail the request.
    /// </remarks>
    private Effort? ResolveEffort(string? storedLevel, string model)
    {
        if (string.IsNullOrWhiteSpace(storedLevel))
        {
            return null;
        }

        var level = storedLevel.Trim().ToLowerInvariant();

        // Prefer what the API reported for this model; fall back to the ID predicate when it is unknown.
        var supportsEffort = Provider.TryGetModelCapability(model)?.SupportsEffort
                             ?? AnthropicModelUtilities.SupportsEffort(model);

        if (!supportsEffort || !AnthropicModelUtilities.IsKnownEffortLevel(level))
        {
            return null;
        }

        return level switch
        {
            "low" => Effort.Low,
            "medium" => Effort.Medium,
            "high" => Effort.High,
            // xhigh and max are not offered: which models accept them cannot be tracked with a list, so a
            // stored value for either is skipped rather than sent.
            _ => null,
        };
    }

    /// <summary>
    /// Translates the stored prompt-caching TTL into a top-level cache-control marker, or <c>null</c> when
    /// caching is off or the stored value is not one Anthropic accepts.
    /// </summary>
    /// <remarks>
    /// No per-model gate: every Claude model supports caching, and a prefix below the model's minimum is
    /// declined silently rather than rejected, so there is nothing to guard against. An unrecognised stored
    /// value is dropped rather than sent, which keeps a hand-edited or future-dated profile from failing
    /// the request.
    /// </remarks>
    private static BetaCacheControlEphemeral? ResolveCacheControl(string? storedTtl)
        => storedTtl?.Trim().ToLowerInvariant() switch
        {
            "5m" => new BetaCacheControlEphemeral { Ttl = Ttl.Ttl5m },
            "1h" => new BetaCacheControlEphemeral { Ttl = Ttl.Ttl1h },
            _ => null,
        };

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
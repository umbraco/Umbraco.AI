using System.Text.RegularExpressions;
using Anthropic.Models.Beta.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

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
    /// </remarks>
    public AnthropicChatCapability(AnthropicProvider provider)
        : this(provider, null)
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
    /// The declaration of which settings each model accepts is written here rather than through
    /// <see cref="IAICapability.GetSettingSupport"/>, because Anthropic's models endpoint reports it as
    /// data: <c>capabilities.effort.supported</c> per model. Only models the API says nothing about fall
    /// back to inferring support from the ID.
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
                BuildSettingSupport(m).ToMetadata()))
            .ToList();
    }

    /// <summary>
    /// Turns a model's reported capabilities into the settings declaration the profile editor reads,
    /// falling back to the ID-based predicate when the API reported nothing for the model.
    /// </summary>
    private static AIModelSettingSupport BuildSettingSupport(AnthropicModelCapability model)
        => (model.SupportsEffort ?? AnthropicModelUtilities.SupportsEffort(model.Id))
            ? AIModelSettingSupport.Default
            : new AIModelSettingSupport
            {
                UnsupportedCapabilitySettings = [nameof(AnthropicChatCapabilitySettings.Effort)],
            };

    /// <inheritdoc />
    protected override IChatClient CreateClient(AnthropicProviderSettings settings, string? modelId)
    {
        var inner = AnthropicProvider.CreateAnthropicClient(settings)
            .Beta.AsIChatClient(modelId);

        // Wrapped innermost, so the sampling parameters are filtered against the target model no matter
        // which caller assembled the ChatOptions. See AnthropicSamplingParameterChatClient.
        return new AnthropicSamplingParameterChatClient(inner, modelId, logger);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Fetches the model list (cached) before handing back the client, so the per-request settings hook can
    /// read the target model's reported capabilities synchronously. A failure here is swallowed: capability
    /// data refines the decision but is not required to make a request, and losing the model list must not
    /// stop chat from working.
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
        catch (Exception)
        {
            // Fall back to inferring support from the model ID.
        }

        return CreateClient(settings, modelId);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Sets <c>output_config.effort</c> through <see cref="ChatOptions.RawRepresentationFactory"/>, the
    /// only channel the SDK's Microsoft.Extensions.AI adapter reads — values placed in
    /// <see cref="ChatOptions.AdditionalProperties"/> are dropped before the request is built.
    /// </para>
    /// <para>
    /// The adapter treats the factory's result as the request base and overwrites only the messages, so
    /// the model and max-tokens values have to be carried into it here or the request would be sent with
    /// whatever this method put there. <c>AnthropicEffortWireTests</c> pins both behaviours down.
    /// </para>
    /// <para>
    /// Skipped when the model does not accept effort at all, or does not accept the configured level —
    /// the same predicates that drive <see cref="GetSettingSupport"/> — so a profile carrying a value the
    /// model rejects cannot fail the request.
    /// </para>
    /// </remarks>
    protected override void ApplyCapabilitySettings(
        AnthropicChatCapabilitySettings capabilitySettings,
        string? modelId,
        ChatOptions options)
    {
        if (string.IsNullOrWhiteSpace(capabilitySettings.Effort))
        {
            return;
        }

        var model = modelId ?? DefaultChatModel;
        var level = capabilitySettings.Effort.Trim().ToLowerInvariant();

        // Prefer what the API reported for this model; fall back to the ID predicate when it is unknown.
        var supportsEffort = Provider.TryGetModelCapability(model)?.SupportsEffort
                             ?? AnthropicModelUtilities.SupportsEffort(model);

        if (!supportsEffort || !AnthropicModelUtilities.IsKnownEffortLevel(level))
        {
            return;
        }

        var effort = level switch
        {
            "low" => Effort.Low,
            "medium" => Effort.Medium,
            "high" => Effort.High,
            // xhigh and max are not offered: which models accept them cannot be tracked with a list, so a
            // stored value for either is skipped rather than sent.
            _ => (Effort?)null,
        };

        if (effort is null)
        {
            return;
        }

        var previousFactory = options.RawRepresentationFactory;
        var maxTokens = options.MaxOutputTokens ?? AdapterDefaultMaxTokens;

        options.RawRepresentationFactory = client =>
        {
            var existing = previousFactory?.Invoke(client) as MessageCreateParams;

            // OutputConfig is init-only, so an existing representation is copied with the effort applied
            // and any sibling output settings preserved.
            var outputConfig = new BetaOutputConfig
            {
                Effort = effort,
                Format = existing?.OutputConfig?.Format,
                TaskBudget = existing?.OutputConfig?.TaskBudget,
            };

            return existing is null
                ? new MessageCreateParams
                {
                    Model = model,
                    MaxTokens = maxTokens,
                    Messages = [],
                    OutputConfig = outputConfig,
                }
                : existing with { OutputConfig = outputConfig };
        };
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
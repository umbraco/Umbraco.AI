using System.Text.RegularExpressions;
using Anthropic.Models.Beta.Messages;
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
    /// Effort is not accepted by Claude 3.x, the base Claude 4 models, Opus 4.1, Sonnet 4.5 or Haiku 4.5,
    /// so it is declared per model — otherwise the profile editor would offer it on a model that rejects
    /// it. That legacy set is closed, so the predicate is a deny-list and an unrecognised model is treated
    /// as supporting effort.
    /// </remarks>
    public override AIModelSettingSupport GetSettingSupport(string modelId)
        => AnthropicModelUtilities.SupportsEffort(modelId)
            ? AIModelSettingSupport.Default
            : new AIModelSettingSupport
            {
                UnsupportedCapabilitySettings = [nameof(AnthropicChatCapabilitySettings.Effort)],
            };

    /// <inheritdoc />
    protected override IChatClient CreateClient(AnthropicProviderSettings settings, string? modelId)
        => AnthropicProvider.CreateAnthropicClient(settings)
            .Beta.AsIChatClient(modelId);

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

        if (!AnthropicModelUtilities.SupportsEffortLevel(model, level))
        {
            return;
        }

        var effort = level switch
        {
            "low" => Effort.Low,
            "medium" => Effort.Medium,
            "high" => Effort.High,
            "xhigh" => Effort.Xhigh,
            "max" => Effort.Max,
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
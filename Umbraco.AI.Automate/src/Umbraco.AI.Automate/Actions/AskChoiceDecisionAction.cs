using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;

#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// An Automate action that asks the AI to pick one of a fixed set of options against an AI
/// decision profile, so an automation can branch on the chosen key (e.g. feeding a Switch step).
/// </summary>
[Action(UmbracoAIAutomateConstants.ActionTypes.AskChoiceDecision, "Ask Pick-One",
    Description = "Asks the AI to pick one of a fixed set of options against an AI decision profile.",
    Group = "AI",
    Icon = "icon-list")]
public sealed class AskChoiceDecisionAction : ActionBase<AskChoiceDecisionSettings, AskChoiceDecisionOutput>
{
    private readonly IAIDecisionService _decisionService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;
    private readonly ILogger<AskChoiceDecisionAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AskChoiceDecisionAction"/> class.
    /// </summary>
    public AskChoiceDecisionAction(
        ActionInfrastructure infrastructure,
        IAIDecisionService decisionService,
        IAIExperimentalFeatures experimentalFeatures,
        ILogger<AskChoiceDecisionAction> logger)
        : base(infrastructure)
    {
        _decisionService = decisionService;
        _experimentalFeatures = experimentalFeatures;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        // Flag off at run time (e.g. flipped without a restart): refuse before any provider
        // call, even though the action is also excluded from discovery at compose time when
        // the flag is off at startup.
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            return ActionResult.Failed(
                new InvalidOperationException("Decision is disabled."),
                StepRunErrorCategory.Validation);
        }

        var settings = context.GetSettings<AskChoiceDecisionSettings>();

        _logger.LogInformation(
            "Automation {AutomationId} / Run {RunId}: Asking pick-one decision",
            context.AutomationId, context.RunId);

        try
        {
            var options = MapOptions(settings.Options);

            var question = new AIChoiceDecisionQuestion
            {
                Instructions = settings.Instructions,
                Context = settings.Context,
                Options = options,
            };

            var response = await _decisionService.AskAsync(
                b =>
                {
                    b.WithAlias("automate-ask-choice-decision");

                    if (settings.ProfileId.HasValue && settings.ProfileId.Value != Guid.Empty)
                    {
                        b.WithProfile(settings.ProfileId.Value);
                    }
                },
                question,
                cancellationToken);

            return Success(new AskChoiceDecisionOutput
            {
                Choice = response.Choice,
                Confidence = response.Confidence,
            });
        }
        catch (ArgumentException ex)
        {
            // Thrown by Core's ValidatingDecisionClient (e.g. fewer than 2 options).
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (InvalidOperationException ex)
        {
            // Thrown by IAIDecisionService when no default Decision profile resolves, or the
            // configured profile isn't a Decision profile.
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (OperationCanceledException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Automation {AutomationId} / Run {RunId}: Ask pick-one decision failed",
                context.AutomationId, context.RunId);
            return ActionResult.Failed(ex, StepRunErrorCategory.Unknown);
        }
    }

    /// <summary>
    /// Maps <see cref="AskChoiceDecisionSettings.Options"/>'s key/value rows into
    /// <see cref="AIDecisionOption"/> entries, in order, trimming each key and value. A blank
    /// value maps to no description. This only maps the shape — entry count, blank-key, and
    /// duplicate-key checks all happen downstream, in Core's Decision validator, when the
    /// resulting question reaches <see cref="IAIDecisionService"/>. Null-safe on purpose: a saved
    /// settings blob can contain a null list entry or a null <c>Key</c>/<c>Value</c> (e.g. hand-edited
    /// JSON, or a row cleared through the property editor), so this maps those to an empty key
    /// rather than throwing — an empty/blank key is then rejected downstream as a validation
    /// error instead of surfacing as an unhandled exception.
    /// </summary>
    private static IReadOnlyList<AIDecisionOption> MapOptions(IReadOnlyList<AskChoiceDecisionOption>? options)
    {
        if (options is null || options.Count == 0)
        {
            return [];
        }

        var mapped = new List<AIDecisionOption>(options.Count);
        foreach (var option in options)
        {
            var key = option?.Key?.Trim() ?? string.Empty;
            var value = option?.Value?.Trim();
            mapped.Add(new AIDecisionOption(key, string.IsNullOrEmpty(value) ? null : value));
        }

        return mapped;
    }
}

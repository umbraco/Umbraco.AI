using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;

#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// An Automate action that asks the AI to score against labelled levels using an AI decision
/// profile, so an automation can branch on the resulting label (e.g. feeding a Switch step).
/// </summary>
[Action(UmbracoAIAutomateConstants.ActionTypes.AskScoreDecision, "Ask Score",
    Description = "Asks the AI to score against labelled levels using an AI decision profile.",
    Group = "AI",
    Icon = "icon-speed-gauge")]
public sealed class AskScoreDecisionAction : ActionBase<AskScoreDecisionSettings, AskScoreDecisionOutput>
{
    private readonly IAIDecisionService _decisionService;
    private readonly IAIExperimentalFeatures _experimentalFeatures;
    private readonly ILogger<AskScoreDecisionAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AskScoreDecisionAction"/> class.
    /// </summary>
    public AskScoreDecisionAction(
        ActionInfrastructure infrastructure,
        IAIDecisionService decisionService,
        IAIExperimentalFeatures experimentalFeatures,
        ILogger<AskScoreDecisionAction> logger)
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

        var settings = context.GetSettings<AskScoreDecisionSettings>();

        _logger.LogInformation(
            "Automation {AutomationId} / Run {RunId}: Asking score decision",
            context.AutomationId, context.RunId);

        try
        {
            var question = new AIScoreDecisionQuestion
            {
                Instructions = settings.Instructions,
                Context = settings.Context,
                Levels = settings.Levels,
            };

            var response = await _decisionService.AskAsync(
                b =>
                {
                    b.WithAlias("automate-ask-score-decision");

                    if (settings.ProfileId.HasValue && settings.ProfileId.Value != Guid.Empty)
                    {
                        b.WithProfile(settings.ProfileId.Value);
                    }
                },
                question,
                cancellationToken);

            return Success(new AskScoreDecisionOutput
            {
                Score = response.Score,
                Level = response.Level,
                Confidence = response.Confidence,
            });
        }
        catch (ArgumentException ex)
        {
            // Thrown by Core's ValidatingDecisionClient (e.g. fewer than 2 levels).
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
                "Automation {AutomationId} / Run {RunId}: Ask score decision failed",
                context.AutomationId, context.RunId);
            return ActionResult.Failed(ex, StepRunErrorCategory.Unknown);
        }
    }
}

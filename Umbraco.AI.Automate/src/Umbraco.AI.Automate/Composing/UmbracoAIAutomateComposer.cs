using Microsoft.Extensions.Configuration;
using Umbraco.AI.Agent.Startup.Configuration;
using Umbraco.AI.Automate.Actions;
using Umbraco.Automate.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

#pragma warning disable UMBRACOAI_DECISION // Decision capability is experimental

namespace Umbraco.AI.Automate.Composing;

/// <summary>
/// Composer for the Umbraco AI Automate package. Triggers and actions are auto-discovered
/// by Umbraco.Automate via the <c>[Trigger]</c> and <c>[Action]</c> attributes.
/// </summary>
[ComposeAfter(typeof(UmbracoAIAgentComposer))]
public class UmbracoAIAutomateComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        // Triggers, actions, and surfaces are auto-discovered via TypeLoader attributes:
        // - [Action] / [Trigger] by Umbraco.Automate
        // - [AIAgentSurface] by Umbraco.AI.Agent
        // Register any additional services needed by actions/triggers here.

        // Compose-time gate for the three Decision actions: when the flag is off at startup,
        // they're excluded from the action picker entirely, rather than merely refusing at run
        // time. Read directly from configuration (not IAIExperimentalFeatures/IOptions) because
        // the DI container isn't built yet at compose time. Each action's ExecuteAsync also
        // checks IAIExperimentalFeatures at run time, for a flag flipped without a restart.
        if (!builder.Config.GetValue<bool>("Umbraco:AI:Experimental:Decision"))
        {
            builder.AutomateActions()
                .Exclude<AskYesNoDecisionAction>()
                .Exclude<AskChoiceDecisionAction>()
                .Exclude<AskScoreDecisionAction>();
        }
    }
}

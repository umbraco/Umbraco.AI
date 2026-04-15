using Umbraco.AI.Agent.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

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
    }
}

using Umbraco.Automate.Core.Triggers;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// A trigger that fires when an AI agent invokes the automation.
/// Provides a message from the agent as output for downstream workflow steps.
/// </summary>
[Trigger(UmbracoAIAutomateConstants.TriggerTypes.AgentTrigger, "AI Agent",
    Description = "Fires when an AI agent triggers the automation.",
    Group = "AI",
    Icon = "icon-bot")]
public sealed class AgentTrigger : TriggerBase<object, AgentTriggerOutput>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentTrigger"/> class.
    /// </summary>
    public AgentTrigger(TriggerInfrastructure infrastructure) : base(infrastructure)
    {
    }
}

using Umbraco.Automate.Core.Triggers;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// A trigger that fires when an AI agent requests the automation to run.
/// Provides a message from the agent as output for downstream workflow steps.
/// </summary>
[Trigger(UmbracoAIAutomateConstants.TriggerTypes.AgentTrigger, "AI Agent Request",
    Description = "Fires when requested by an AI agent during a conversation.",
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

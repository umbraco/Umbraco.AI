using Umbraco.Automate.Core.Settings;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Output produced by the AI agent trigger, available as bindings in downstream workflow steps.
/// </summary>
public class AgentTriggerOutput
{
    /// <summary>
    /// The message or data provided by the AI agent when triggering the automation.
    /// </summary>
    [Field(Label = "Message", Description = "The message provided by the AI agent.")]
    public string Message { get; set; } = string.Empty;
}

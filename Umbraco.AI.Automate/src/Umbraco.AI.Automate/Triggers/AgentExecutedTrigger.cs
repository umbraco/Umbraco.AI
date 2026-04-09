using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Automate.Core.Triggers;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Fires when an AI agent completes execution.
/// Produces one <see cref="TriggerEvent"/> per agent execution.
/// Settings-based filtering (e.g. by agent alias) is handled by the Automate infrastructure
/// when matching trigger events against configured automations.
/// </summary>
[Trigger(UmbracoAIAutomateConstants.TriggerTypes.AgentExecuted, "AI Agent Executed",
    Description = "Fires when an AI agent completes execution.",
    Group = "AI",
    Icon = "icon-bot")]
public sealed class AgentExecutedTrigger
    : NotificationTriggerBase<AgentExecutedTriggerSettings, AgentExecutedTriggerOutput, AIAgentExecutedNotification>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutedTrigger"/> class.
    /// </summary>
    public AgentExecutedTrigger(TriggerInfrastructure infrastructure) : base(infrastructure)
    {
    }

    /// <inheritdoc />
    public override IEnumerable<TriggerEvent> MapEvent(AIAgentExecutedNotification notification)
    {
        yield return new TriggerEvent<AgentExecutedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "ai-agent",
            InitiatorId = notification.Agent.Id.ToString(),
            IdempotencyKey = GenerateIdempotencyKey(notification.Agent.Id),
            Output = new AgentExecutedTriggerOutput
            {
                AgentId = notification.Agent.Id,
                AgentAlias = notification.Agent.Alias,
                AgentName = notification.Agent.Name ?? string.Empty,
                IsSuccess = notification.IsSuccess,
                DurationMs = notification.Duration.TotalMilliseconds,
            },
        };
    }
}

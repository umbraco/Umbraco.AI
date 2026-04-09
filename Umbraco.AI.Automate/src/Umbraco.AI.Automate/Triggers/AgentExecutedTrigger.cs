using Json.Schema;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Helpers;
using Umbraco.Automate.Core.Triggers;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Fires when an AI agent completes execution.
/// Uses dynamic output schema resolved from the agent's configured output schema.
/// </summary>
[Trigger(UmbracoAIAutomateConstants.TriggerTypes.AgentExecuted, "AI Agent Executed",
    Description = "Fires when an AI agent completes execution.",
    Group = "AI",
    Icon = "icon-bot")]
public sealed class AgentExecutedTrigger
    : NotificationTriggerBase<AgentExecutedTriggerSettings, object, AIAgentExecutedNotification>
{
    private readonly IAIAgentService _agentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutedTrigger"/> class.
    /// </summary>
    public AgentExecutedTrigger(TriggerInfrastructure infrastructure, IAIAgentService agentService)
        : base(infrastructure)
    {
        _agentService = agentService;
    }

    /// <inheritdoc />
    public override bool HasDynamicOutputSchema => true;

    /// <inheritdoc />
    protected override Task<JsonSchema?> GetOutputSchemaAsync(
        AgentExecutedTriggerSettings? settings,
        CancellationToken cancellationToken = default)
    {
        if (settings?.AgentId is null || settings.AgentId == Guid.Empty)
        {
            return Task.FromResult<JsonSchema?>(null);
        }

        return AgentOutputSchemaHelper.GetOutputSchemaAsync(_agentService, settings.AgentId.Value, cancellationToken);
    }

    /// <inheritdoc />
    public override IEnumerable<TriggerEvent> MapEvent(AIAgentExecutedNotification notification)
    {
        yield return new TriggerEvent<object>
        {
            TriggerAlias = Alias,
            InitiatorType = "ai-agent",
            InitiatorId = notification.Agent.Id.ToString(),
            IdempotencyKey = GenerateIdempotencyKey(notification.Agent.Id),
            Output = new
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

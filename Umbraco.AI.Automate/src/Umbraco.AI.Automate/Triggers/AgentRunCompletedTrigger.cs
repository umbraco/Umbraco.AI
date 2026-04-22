using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Core.Triggers;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Fires when an AI agent run completes successfully. Observes <see cref="AIAgentExecutedNotification"/>
/// and ignores failed runs (those are dispatched by <see cref="AgentRunFailedTrigger"/>).
/// </summary>
[Trigger(UmbracoAIAutomateConstants.TriggerTypes.AgentRunCompleted, "AI Agent Run Completed",
    Description = "Fires when an AI agent run completes successfully.",
    Group = "AI",
    Icon = "icon-bot")]
public sealed class AgentRunCompletedTrigger
    : NotificationTriggerBase<AgentRunCompletedTriggerSettings, AgentRunCompletedTriggerOutput, AIAgentExecutedNotification>
{
    private readonly IExecutionContextAccessor _executionContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunCompletedTrigger"/> class.
    /// </summary>
    public AgentRunCompletedTrigger(
        TriggerInfrastructure infrastructure,
        IExecutionContextAccessor executionContextAccessor)
        : base(infrastructure)
    {
        _executionContextAccessor = executionContextAccessor;
    }

    /// <inheritdoc />
    public override IEnumerable<TriggerEvent> MapEvent(AIAgentExecutedNotification notification)
    {
        if (!notification.IsSuccess)
        {
            yield break;
        }

        // Loop prevention — DO NOT REMOVE WITHOUT A REPLACEMENT.
        //
        // If the agent run happened inside an Automate workflow (typically via RunAgentAction),
        // firing this trigger would schedule another workflow run, which can itself run an agent,
        // which fires this trigger, and so on. There is no bound on that recursion — the existing
        // RunAgentAction depth guard only applies when the trigger output carries _aiAgentDepth,
        // which neither we nor the CMS notification pipeline set.
        //
        // Rationale for this check: these triggers are intended to observe "external" agent runs
        // (chat UI, Management API, background jobs). Runs driven by a workflow action are already
        // visible to that workflow's own steps, so there is no observability gap. If someone has
        // a legitimate need to chain workflows via agent runs, that should be a conscious opt-in.
        //
        // If Umbraco.Automate later adds a first-class "skip self-triggered" mechanism, replace
        // this with that mechanism.
        if (_executionContextAccessor.ExecutionContext is not null)
        {
            yield break;
        }

        yield return new TriggerEvent<AgentRunCompletedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "ai-agent",
            InitiatorId = notification.Agent.Id.ToString(),
            Output = new AgentRunCompletedTriggerOutput
            {
                AgentId = notification.Agent.Id,
                AgentAlias = notification.Agent.Alias,
                AgentName = notification.Agent.Name,
                Prompt = AgentRunTriggerHelper.GetLastUserPrompt(notification.ChatMessages),
                Response = notification.ResponseText ?? string.Empty,
                DurationSeconds = notification.Duration.TotalSeconds,
            },
        };
    }
}

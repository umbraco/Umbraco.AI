using System.Linq;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Core.Triggers;

namespace Umbraco.AI.Automate.Triggers;

/// <summary>
/// Fires when an AI agent run fails. Observes <see cref="AIAgentExecutedNotification"/>
/// and ignores successful runs (those are dispatched by <see cref="AgentRunCompletedTrigger"/>).
/// </summary>
[Trigger(UmbracoAIAutomateConstants.TriggerTypes.AgentRunFailed, "AI Agent Run Failed",
    Description = "Fires when an AI agent run fails.",
    Group = "AI",
    Icon = "icon-alert")]
public sealed class AgentRunFailedTrigger
    : NotificationTriggerBase<AgentRunFailedTriggerSettings, AgentRunFailedTriggerOutput, AIAgentExecutedNotification>
{
    private readonly IExecutionContextAccessor _executionContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunFailedTrigger"/> class.
    /// </summary>
    public AgentRunFailedTrigger(
        TriggerInfrastructure infrastructure,
        IExecutionContextAccessor executionContextAccessor)
        : base(infrastructure)
    {
        _executionContextAccessor = executionContextAccessor;
    }

    /// <inheritdoc />
    public override IEnumerable<TriggerEvent> MapEvent(AIAgentExecutedNotification notification)
    {
        if (notification.IsSuccess)
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

        yield return new TriggerEvent<AgentRunFailedTriggerOutput>
        {
            TriggerAlias = Alias,
            InitiatorType = "ai-agent",
            InitiatorId = notification.Agent.Id.ToString(),
            Output = new AgentRunFailedTriggerOutput
            {
                AgentId = notification.Agent.Id,
                AgentAlias = notification.Agent.Alias,
                AgentName = notification.Agent.Name,
                Prompt = AgentRunTriggerHelper.GetLastUserPrompt(notification.ChatMessages),
                DurationSeconds = notification.Duration.TotalSeconds,
                ErrorMessage = ResolveErrorMessage(notification),
                ErrorType = notification.Exception?.GetType().FullName,
            },
        };
    }

    private static string ResolveErrorMessage(AIAgentExecutedNotification notification)
    {
        if (!string.IsNullOrEmpty(notification.Exception?.Message))
        {
            return notification.Exception.Message;
        }

        var firstEventMessage = notification.Messages.GetAll().FirstOrDefault()?.Message;
        if (!string.IsNullOrEmpty(firstEventMessage))
        {
            return firstEventMessage;
        }

        return "Agent run failed.";
    }
}

using System.Reflection;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Triggers;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Automate.Tests.Unit.Triggers;

public class AgentRunFailedTriggerTests
{
    private static readonly Guid TestAgentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void MapEvent_FailedRunWithException_ProducesEventWithErrorDetails()
    {
        var trigger = CreateTrigger();
        var exception = new InvalidOperationException("Agent blew up");
        var notification = CreateNotification(
            isSuccess: false,
            prompt: "Do the thing",
            duration: TimeSpan.FromMilliseconds(500),
            exception: exception);

        var events = trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        var typed = events[0].ShouldBeOfType<TriggerEvent<AgentRunFailedTriggerOutput>>();
        typed.TriggerAlias.ShouldBe(UmbracoAIAutomateConstants.TriggerTypes.AgentRunFailed);
        typed.InitiatorType.ShouldBe("ai-agent");
        typed.InitiatorId.ShouldBe(TestAgentId.ToString());
        typed.Output.AgentId.ShouldBe(TestAgentId);
        typed.Output.Prompt.ShouldBe("Do the thing");
        typed.Output.DurationSeconds.ShouldBe(0.5, tolerance: 0.001);
        typed.Output.ErrorMessage.ShouldBe("Agent blew up");
        typed.Output.ErrorType.ShouldBe(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public void MapEvent_SuccessfulRun_ProducesNoEvents()
    {
        var trigger = CreateTrigger();
        var notification = CreateNotification(isSuccess: true);

        trigger.MapEvent(notification).ShouldBeEmpty();
    }

    [Fact]
    public void MapEvent_FailedRunWithoutExceptionButWithEventMessage_UsesEventMessage()
    {
        var trigger = CreateTrigger();
        var eventMessages = new EventMessages();
        eventMessages.Add(new EventMessage("Agent", "The profile is missing.", EventMessageType.Error));

        var notification = CreateNotification(
            isSuccess: false,
            exception: null,
            eventMessages: eventMessages);

        var typed = trigger.MapEvent(notification).Single()
            .ShouldBeOfType<TriggerEvent<AgentRunFailedTriggerOutput>>();

        typed.Output.ErrorMessage.ShouldBe("The profile is missing.");
        typed.Output.ErrorType.ShouldBeNull();
    }

    [Fact]
    public void MapEvent_FailedRunWithNoExceptionAndNoMessages_UsesGenericFallback()
    {
        var trigger = CreateTrigger();
        var notification = CreateNotification(isSuccess: false, exception: null);

        var typed = trigger.MapEvent(notification).Single()
            .ShouldBeOfType<TriggerEvent<AgentRunFailedTriggerOutput>>();

        typed.Output.ErrorMessage.ShouldBe("Agent run failed.");
    }

    [Fact]
    public void MapEvent_AgentRunInsideAutomationWorkflow_ProducesNoEvents()
    {
        // Loop prevention: a failed agent run inside a workflow must not re-fire the trigger.
        // Otherwise a workflow that reacts to failures by running another agent (e.g. a
        // retry/remediation flow) would spiral on repeated failures. See the trigger for
        // full rationale.
        var trigger = CreateTrigger();
        var notification = CreateNotification(isSuccess: false, exception: new InvalidOperationException("boom"));

        using (AutomateAgentRunScope.Enter())
        {
            trigger.MapEvent(notification).ShouldBeEmpty();
        }
    }

    private static AgentRunFailedTrigger CreateTrigger()
    {
        var infrastructure = new TriggerInfrastructure(new Mock<IEditableModelResolver>().Object);
        return new AgentRunFailedTrigger(infrastructure);
    }

    private static AIAgentExecutedNotification CreateNotification(
        bool isSuccess,
        string prompt = "Hello",
        TimeSpan? duration = null,
        Exception? exception = null,
        EventMessages? eventMessages = null)
    {
        var agent = new AIAgent { Alias = "test-agent", Name = "Test Agent" };
        typeof(AIAgent).GetProperty(nameof(AIAgent.Id), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(agent, TestAgentId);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, prompt),
        };

        return new AIAgentExecutedNotification(
            agent,
            messages,
            duration ?? TimeSpan.FromSeconds(1),
            isSuccess,
            eventMessages ?? new EventMessages())
        {
            Exception = exception,
        };
    }
}

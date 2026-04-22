using System.Reflection;
using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Triggers;
using Umbraco.Automate.Core.Execution;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.Triggers;
using Umbraco.Cms.Core.Events;
using Xunit;
using AIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Automate.Tests.Unit.Triggers;

public class AgentRunCompletedTriggerTests
{
    private static readonly Guid TestAgentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void MapEvent_SuccessfulRun_ProducesOneEvent()
    {
        var trigger = CreateTrigger();
        var notification = CreateNotification(
            isSuccess: true,
            prompt: "Summarise the weather",
            responseText: "It is sunny.",
            duration: TimeSpan.FromMilliseconds(1250));

        var events = trigger.MapEvent(notification).ToList();

        events.ShouldHaveSingleItem();
        var typed = events[0].ShouldBeOfType<TriggerEvent<AgentRunCompletedTriggerOutput>>();
        typed.TriggerAlias.ShouldBe(UmbracoAIAutomateConstants.TriggerTypes.AgentRunCompleted);
        typed.InitiatorType.ShouldBe("ai-agent");
        typed.InitiatorId.ShouldBe(TestAgentId.ToString());
        typed.Output.AgentId.ShouldBe(TestAgentId);
        typed.Output.AgentAlias.ShouldBe("test-agent");
        typed.Output.AgentName.ShouldBe("Test Agent");
        typed.Output.Prompt.ShouldBe("Summarise the weather");
        typed.Output.Response.ShouldBe("It is sunny.");
        typed.Output.DurationSeconds.ShouldBe(1.25, tolerance: 0.001);
    }

    [Fact]
    public void MapEvent_FailedRun_ProducesNoEvents()
    {
        var trigger = CreateTrigger();
        var notification = CreateNotification(isSuccess: false);

        trigger.MapEvent(notification).ShouldBeEmpty();
    }

    [Fact]
    public void MapEvent_StreamingRunWithoutResponseText_ReturnsEmptyResponse()
    {
        var trigger = CreateTrigger();
        var notification = CreateNotification(
            isSuccess: true,
            prompt: "Hi",
            responseText: null);

        var typed = trigger.MapEvent(notification).Single()
            .ShouldBeOfType<TriggerEvent<AgentRunCompletedTriggerOutput>>();

        typed.Output.Response.ShouldBe(string.Empty);
    }

    [Fact]
    public void MapEvent_NoUserMessages_PromptIsEmpty()
    {
        var trigger = CreateTrigger();
        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are helpful."),
        };
        var notification = CreateNotification(isSuccess: true, chatMessages: chatMessages);

        var typed = trigger.MapEvent(notification).Single()
            .ShouldBeOfType<TriggerEvent<AgentRunCompletedTriggerOutput>>();

        typed.Output.Prompt.ShouldBe(string.Empty);
    }

    [Fact]
    public void MapEvent_AgentRunInsideAutomationWorkflow_ProducesNoEvents()
    {
        // Loop prevention: runs that happen inside an active Automate workflow must not
        // re-fire the trigger, otherwise an agent action in a workflow would cause unbounded
        // recursion. Covered by the IExecutionContextAccessor check in the trigger.
        var trigger = CreateTrigger(insideWorkflow: true);
        var notification = CreateNotification(isSuccess: true);

        trigger.MapEvent(notification).ShouldBeEmpty();
    }

    private static AgentRunCompletedTrigger CreateTrigger(bool insideWorkflow = false)
    {
        var infrastructure = new TriggerInfrastructure(new Mock<IEditableModelResolver>().Object);
        var accessor = new Mock<IExecutionContextAccessor>();
        accessor.Setup(a => a.ExecutionContext).Returns(insideWorkflow ? CreateFakeContext() : null);
        return new AgentRunCompletedTrigger(infrastructure, accessor.Object);
    }

    private static AutomationExecutionContext CreateFakeContext() => new()
    {
        ServiceAccountKey = Guid.NewGuid(),
        WorkspaceId = Guid.NewGuid(),
        WorkspaceName = "Test Workspace",
        AutomationId = Guid.NewGuid(),
        AutomationName = "Test Automation",
        RunId = Guid.NewGuid(),
        InitiatorType = "user",
        AllowedConnections = Array.Empty<Guid>(),
    };

    private static AIAgentExecutedNotification CreateNotification(
        bool isSuccess,
        string prompt = "Hello",
        string? responseText = "Hi there",
        TimeSpan? duration = null,
        IReadOnlyList<ChatMessage>? chatMessages = null)
    {
        var agent = new AIAgent { Alias = "test-agent", Name = "Test Agent" };
        typeof(AIAgent).GetProperty(nameof(AIAgent.Id), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(agent, TestAgentId);

        var messages = chatMessages ?? new List<ChatMessage>
        {
            new(ChatRole.User, prompt),
        };

        return new AIAgentExecutedNotification(
            agent,
            messages,
            duration ?? TimeSpan.FromSeconds(1),
            isSuccess,
            new EventMessages())
        {
            ResponseText = responseText,
        };
    }
}

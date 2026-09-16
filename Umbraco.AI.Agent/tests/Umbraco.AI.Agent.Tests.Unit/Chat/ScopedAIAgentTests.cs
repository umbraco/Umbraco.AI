using Shouldly;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// Tests for <see cref="ScopedAIAgent.StageSystemMessageParts"/>.
/// </summary>
public class ScopedAIAgentTests
{
    [Fact]
    public void StageSystemMessageParts_WithParts_StagesThePendingSystemMessage()
    {
        // Arrange
        var context = new AIRuntimeContext([]);
        context.SystemMessageParts.Add("## Current Entity Context");
        context.SystemMessageParts.Add("- Page: About Us");

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert
        context.TryGetValue<string>(AgentConstants.ContextKeys.PendingSystemMessage, out var staged).ShouldBeTrue();
        staged.ShouldBe("## Current Entity Context\n\n- Page: About Us");
    }

    [Fact]
    public void StageSystemMessageParts_WithNoParts_StagesNothing()
    {
        // Arrange
        var context = new AIRuntimeContext([]);

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert
        context.TryGetValue<string>(AgentConstants.ContextKeys.PendingSystemMessage, out _).ShouldBeFalse();
    }
}

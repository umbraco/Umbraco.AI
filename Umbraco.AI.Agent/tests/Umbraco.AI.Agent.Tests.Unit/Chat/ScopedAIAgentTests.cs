using Shouldly;
using Umbraco.AI.Agent.Core.Chat;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;
using AgentConstants = Umbraco.AI.Agent.Core.Constants;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// Tests for <see cref="ScopedAIAgent.StageSystemMessageParts"/>.
/// </summary>
public class ScopedAIAgentTests
{
    [Fact]
    public void StageSystemMessageParts_WithParts_DeclaresPendingSystemMessageAsALogKey()
    {
        // Arrange
        var context = new AIRuntimeContext([]);
        context.SystemMessageParts.Add("## Current Entity Context\n- Page: About Us");

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert -- declared so the shared, cross-provider audit-logging client (which has no safe way to
        // single this content out of ChatOptions.Instructions -- see umbraco/Umbraco.AI#382) captures it
        // under its own name instead of silently dropping it.
        context.TryGetValue<string[]>(CoreConstants.ContextKeys.LogKeys, out var logKeys).ShouldBeTrue();
        logKeys.ShouldContain(AgentConstants.ContextKeys.PendingSystemMessage);
    }

    [Fact]
    public void StageSystemMessageParts_WithParts_PreservesExistingLogKeys()
    {
        // Arrange -- a caller (e.g. AIAgentService) may have already declared its own keys (RunId, ThreadId)
        var context = new AIRuntimeContext([]);
        context.SetValue(CoreConstants.ContextKeys.LogKeys, new[] { "RunId", "ThreadId" });
        context.SystemMessageParts.Add("context");

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert
        context.TryGetValue<string[]>(CoreConstants.ContextKeys.LogKeys, out var logKeys).ShouldBeTrue();
        logKeys.ShouldBe(["RunId", "ThreadId", AgentConstants.ContextKeys.PendingSystemMessage]);
    }

    [Fact]
    public void StageSystemMessageParts_WithNoParts_DoesNotDeclareALogKey()
    {
        // Arrange -- nothing staged, so nothing to declare
        var context = new AIRuntimeContext([]);

        // Act
        ScopedAIAgent.StageSystemMessageParts(context);

        // Assert
        context.TryGetValue<string[]>(CoreConstants.ContextKeys.LogKeys, out _).ShouldBeFalse();
    }
}

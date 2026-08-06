using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Core.Chat;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// Tests for where the runtime-context system prompt lands in the list actually sent to the model.
/// It has to be index 0 of history plus the new turn: a block that slides further along with each turn
/// moves the point where consecutive requests diverge back to the start, which defeats prompt caching.
/// </summary>
public class AIAgentSystemMessageChatClientTests
{
    private const string SystemPrompt = "## Current User\n- Name: Administrator";

    [Fact]
    public void Inject_WithStoredHistoryAndNewTurn_PutsTheBlockFirst()
    {
        // Arrange — what a server-persisted surface sends on turn two: history, then the new question.
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Say apple"),
            new(ChatRole.Assistant, "apple"),
            new(ChatRole.User, "Now say pear"),
        };

        // Act
        var result = AIAgentSystemMessageChatClient.Inject(messages, SystemPrompt);

        // Assert
        result.Select(m => m.Role).ShouldBe([ChatRole.System, ChatRole.User, ChatRole.Assistant, ChatRole.User]);
        result[0].Text.ShouldBe(SystemPrompt);
    }

    [Fact]
    public void Inject_AcrossTurns_KeepsAStableLeadingPrefix()
    {
        // Arrange — the caching property itself: turn three must start with everything turn two started
        // with. This is what the old agent-layer injection broke, by moving the block each turn.
        var turnTwo = AIAgentSystemMessageChatClient.Inject(
            [new(ChatRole.User, "Say apple"), new(ChatRole.Assistant, "apple"), new(ChatRole.User, "Now say pear")],
            SystemPrompt);

        var turnThree = AIAgentSystemMessageChatClient.Inject(
            [
                new(ChatRole.User, "Say apple"),
                new(ChatRole.Assistant, "apple"),
                new(ChatRole.User, "Now say pear"),
                new(ChatRole.Assistant, "pear"),
                new(ChatRole.User, "And plum"),
            ],
            SystemPrompt);

        // Act
        var sharedPrefix = turnTwo
            .Zip(turnThree, (a, b) => (a.Role == b.Role && a.Text == b.Text))
            .TakeWhile(same => same)
            .Count();

        // Assert — every message of the earlier request is reusable, not just the first couple.
        sharedPrefix.ShouldBe(turnTwo.Count);
    }

    [Fact]
    public void Inject_WithAnExistingLeadingSystemMessage_FoldsIntoItRatherThanAddingASecond()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "## Context\nBrand guidelines"),
            new(ChatRole.User, "Say apple"),
        };

        // Act
        var result = AIAgentSystemMessageChatClient.Inject(messages, SystemPrompt);

        // Assert — runtime context leads, whatever was already there follows.
        result.Count(m => m.Role == ChatRole.System).ShouldBe(1);
        result[0].Text.ShouldBe($"{SystemPrompt}\n\n## Context\nBrand guidelines");
    }

    [Fact]
    public void Inject_CalledTwice_DoesNotStackASecondCopy()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Say apple") };

        // Act
        var once = AIAgentSystemMessageChatClient.Inject(messages, SystemPrompt);
        var twice = AIAgentSystemMessageChatClient.Inject(once, SystemPrompt);

        // Assert
        twice.Count(m => m.Role == ChatRole.System).ShouldBe(1);
        twice.Count.ShouldBe(2);
    }

    [Fact]
    public void Inject_WithATrailingSystemMessage_StillLeadsWithTheBlock()
    {
        // Arrange — a system message elsewhere in the list is not the head, so it must not be folded into.
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Say apple"),
            new(ChatRole.System, "stale block from an older turn"),
        };

        // Act
        var result = AIAgentSystemMessageChatClient.Inject(messages, SystemPrompt);

        // Assert
        result[0].Role.ShouldBe(ChatRole.System);
        result[0].Text.ShouldBe(SystemPrompt);
        result.Count.ShouldBe(3);
    }
}

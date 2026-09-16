using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Core.Chat;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Chat;

/// <summary>
/// Tests for where the agent's stable instructions and the volatile runtime-context prompt each land in
/// the request actually sent to the model (umbraco/Umbraco.AI#382). Stable instructions must lead the
/// message list (index 0) -- provider prompt caching only reuses a request whose leading tokens match the
/// previous one -- while the volatile prompt must land in a new message at the END of the list, after
/// everything else, so it never poisons that cacheable prefix.
/// </summary>
public class AIAgentSystemMessageChatClientTests
{
    private const string StableInstructions = "You are a helpful content editing assistant.";
    private const string VolatilePrompt = "## Current User\n- Name: Administrator";

    [Fact]
    public void Inject_WithStableInstructions_PutsThemAtIndexZeroAndClearsOptions()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Say apple") };
        var options = new ChatOptions { Instructions = StableInstructions };

        // Act
        var (resultMessages, resultOptions) = AIAgentSystemMessageChatClient.Inject(messages, options, null);

        // Assert -- stable instructions lead the message list...
        resultMessages.Select(m => m.Role).ShouldBe([ChatRole.System, ChatRole.User]);
        resultMessages[0].Text.ShouldBe(StableInstructions);
        // ...and are not left behind in Instructions too (would duplicate them on the wire).
        resultOptions!.Instructions.ShouldBeNull();
    }

    [Fact]
    public void Inject_WithVolatilePromptOnly_AppendsItAsATrailingMessageAndLeavesOptionsAlone()
    {
        // Arrange -- no agent instructions configured, only runtime context to inject
        var messages = new List<ChatMessage> { new(ChatRole.User, "Say apple") };

        // Act
        var (resultMessages, resultOptions) = AIAgentSystemMessageChatClient.Inject(messages, null, VolatilePrompt);

        // Assert
        resultMessages.Last().Role.ShouldBe(ChatRole.System);
        resultMessages.Last().Text.ShouldBe(VolatilePrompt);
        resultOptions.ShouldBeNull();
    }

    [Fact]
    public void Inject_WithBothStableAndVolatile_StableLeadsMessagesVolatileTrailsMessages()
    {
        // Arrange -- the normal case: an agent with its own instructions, running with live entity context
        var messages = new List<ChatMessage> { new(ChatRole.User, "Say apple") };
        var options = new ChatOptions { Instructions = StableInstructions };

        // Act
        var (resultMessages, resultOptions) = AIAgentSystemMessageChatClient.Inject(messages, options, VolatilePrompt);

        // Assert -- the provider adapter pulls every system-role message out of the list, in list order,
        // so this order puts the stable block first and the volatile one last on the wire.
        resultMessages[0].Role.ShouldBe(ChatRole.System);
        resultMessages[0].Text.ShouldBe(StableInstructions);
        resultMessages.Last().Role.ShouldBe(ChatRole.System);
        resultMessages.Last().Text.ShouldBe(VolatilePrompt);
        resultOptions!.Instructions.ShouldBeNull();
    }

    [Fact]
    public void Inject_AcrossTurns_KeepsAStableLeadingMessagePrefix()
    {
        // Arrange -- the caching property itself: turn three's message list must start with everything
        // turn two's did, regardless of what the (per-turn, non-cacheable) volatile prompt is doing.
        var (turnTwo, _) = AIAgentSystemMessageChatClient.Inject(
            [new(ChatRole.User, "Say apple"), new(ChatRole.Assistant, "apple"), new(ChatRole.User, "Now say pear")],
            new ChatOptions { Instructions = StableInstructions },
            "entity: v1");

        var (turnThree, _) = AIAgentSystemMessageChatClient.Inject(
            [
                new(ChatRole.System, StableInstructions), // already injected on turn two, carried forward
                new(ChatRole.User, "Say apple"),
                new(ChatRole.Assistant, "apple"),
                new(ChatRole.User, "Now say pear"),
                new(ChatRole.Assistant, "pear"),
                new(ChatRole.User, "And plum"),
            ],
            new ChatOptions { Instructions = StableInstructions },
            "entity: v2");

        // Act -- compare everything up to (but not including) the trailing volatile message, which is
        // expected to differ between the two turns.
        var turnTwoLeading = turnTwo.Take(turnTwo.Count - 1).ToList();
        var turnThreeLeading = turnThree.Take(turnThree.Count - 1).ToList();
        var sharedPrefix = turnTwoLeading
            .Zip(turnThreeLeading, (a, b) => a.Role == b.Role && a.Text == b.Text)
            .TakeWhile(same => same)
            .Count();

        // Assert -- every message of the earlier request is reusable, not just the first couple, even
        // though the volatile entity value changed between the two turns.
        sharedPrefix.ShouldBe(turnTwoLeading.Count);
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
        var options = new ChatOptions { Instructions = StableInstructions };

        // Act
        var (resultMessages, _) = AIAgentSystemMessageChatClient.Inject(messages, options, null);

        // Assert -- stable instructions lead, whatever was already there follows.
        resultMessages.Count(m => m.Role == ChatRole.System).ShouldBe(1);
        resultMessages[0].Text.ShouldBe($"{StableInstructions}\n\n## Context\nBrand guidelines");
    }

    [Fact]
    public void Inject_CalledAgainOnAResumedTurn_DoesNotStackASecondCopyOfInstructions()
    {
        // Arrange -- options.Instructions is re-supplied fresh (from the agent's fixed config) on every
        // turn, but the message list already carries the prior turn's injection forward.
        var messages = new List<ChatMessage> { new(ChatRole.User, "Say apple") };
        var (once, _) = AIAgentSystemMessageChatClient.Inject(
            messages, new ChatOptions { Instructions = StableInstructions }, null);

        // Act -- a resumed/retried run passes the same (now-injected) messages back in, with Instructions
        // set fresh again from the agent's config, same as ChatClientAgent would supply it.
        var (twice, twiceOptions) = AIAgentSystemMessageChatClient.Inject(
            once, new ChatOptions { Instructions = StableInstructions }, null);

        // Assert
        twice.Count(m => m.Role == ChatRole.System).ShouldBe(1);
        twice.Count.ShouldBe(2);
        // Instructions is still cleared even though nothing needed to move this time.
        twiceOptions!.Instructions.ShouldBeNull();
    }

    [Fact]
    public void Inject_WithATrailingSystemMessage_StillLeadsWithTheStableBlock()
    {
        // Arrange -- a system message elsewhere in the list is not the head, so it must not be folded into.
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Say apple"),
            new(ChatRole.System, "stale block from an older turn"),
        };
        var options = new ChatOptions { Instructions = StableInstructions };

        // Act
        var (resultMessages, _) = AIAgentSystemMessageChatClient.Inject(messages, options, null);

        // Assert
        resultMessages[0].Role.ShouldBe(ChatRole.System);
        resultMessages[0].Text.ShouldBe(StableInstructions);
        resultMessages.Count.ShouldBe(3);
    }

    [Fact]
    public void Inject_WithNeitherStableNorVolatileContent_ReturnsInputUnchanged()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "Say apple") };

        // Act
        var (resultMessages, resultOptions) = AIAgentSystemMessageChatClient.Inject(messages, null, null);

        // Assert
        resultMessages.ShouldBeSameAs(messages);
        resultOptions.ShouldBeNull();
    }
}

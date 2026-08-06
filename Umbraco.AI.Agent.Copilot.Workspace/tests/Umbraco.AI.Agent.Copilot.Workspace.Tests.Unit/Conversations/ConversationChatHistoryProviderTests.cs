using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// Tests for what <see cref="ConversationChatHistoryProvider"/> commits to the durable history after a run.
/// The runtime-context system message is re-injected on every run, so storing it would stack an identical
/// block per turn and replay all of them back to the model on the next one.
/// </summary>
public class ConversationChatHistoryProviderTests
{
    [Fact]
    public void ToStoredMessages_DropsTheInjectedSystemMessage()
    {
        // Arrange — a normal turn: the agent layer prepends the runtime context to the user's message.
        ChatMessage[] request =
        [
            new(ChatRole.System, "## Current User\n- Key: 1e70f841"),
            new(ChatRole.User, "Name three colours"),
        ];
        ChatMessage[] response = [new(ChatRole.Assistant, "Red, blue, green.")];

        // Act
        var stored = ConversationChatHistoryProvider.ToStoredMessages(request, response);

        // Assert
        stored.Select(m => m.Role).ShouldBe(["user", "assistant"]);
        stored.ShouldNotContain(m => m.Role == "system");
    }

    [Fact]
    public void ToStoredMessages_KeepsToolAndAssistantMessages()
    {
        // Arrange — only system messages are dropped; a tool-using turn is stored whole.
        ChatMessage[] request = [new(ChatRole.User, "What is the weather?")];
        ChatMessage[] response =
        [
            new(ChatRole.Assistant, "Checking..."),
            new(ChatRole.Tool, "{\"temp\":21}"),
            new(ChatRole.Assistant, "It is 21 degrees."),
        ];

        // Act
        var stored = ConversationChatHistoryProvider.ToStoredMessages(request, response);

        // Assert
        stored.Select(m => m.Role).ShouldBe(["user", "assistant", "tool", "assistant"]);
    }

    [Fact]
    public void ToStoredMessages_WithNoResponse_StillStoresTheInboundTurn()
    {
        // Arrange
        ChatMessage[] request = [new(ChatRole.User, "Hello")];

        // Act
        var stored = ConversationChatHistoryProvider.ToStoredMessages(request, null);

        // Assert
        stored.Count.ShouldBe(1);
        stored[0].ContentText.ShouldBe("Hello");
    }

    [Fact]
    public void ToStoredMessages_WithOnlyASystemMessage_StoresNothing()
    {
        // Arrange — a run that contributes nothing but context must not bump the conversation.
        ChatMessage[] request = [new(ChatRole.System, "## Current User")];

        // Act
        var stored = ConversationChatHistoryProvider.ToStoredMessages(request, null);

        // Assert
        stored.ShouldBeEmpty();
    }
}

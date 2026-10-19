using Microsoft.Extensions.AI;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.InlineChat;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

public class AIStarterPromptSuggesterTests
{
    private readonly Mock<IAIChatService> _chatServiceMock = new();
    private readonly AIStarterPromptSuggester _suggester;

    public AIStarterPromptSuggesterTests()
    {
        _suggester = new AIStarterPromptSuggester(_chatServiceMock.Object);
    }

    [Fact]
    public async Task SuggestStartersAsync_WithMoreThanFourSuggestions_ClampsToFour()
    {
        // Arrange
        var agent = CreateAgent();
        SetupChatResponse("""{"starters":["One","Two","Three","Four","Five","Six"]}""");

        // Act
        var result = await _suggester.SuggestStartersAsync(agent);

        // Assert
        result.Count.ShouldBe(4);
        result.ShouldBe(["One", "Two", "Three", "Four"]);
    }

    [Fact]
    public async Task SuggestStartersAsync_WithSuggestionOverTwoHundredChars_ClampsLength()
    {
        // Arrange
        var agent = CreateAgent();
        var tooLong = new string('a', 250);
        SetupChatResponse($$"""{"starters":["{{tooLong}}"]}""");

        // Act
        var result = await _suggester.SuggestStartersAsync(agent);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Length.ShouldBe(200);
    }

    [Fact]
    public async Task SuggestStartersAsync_WithEmptyStartersArray_ThrowsInvalidOperationException()
    {
        // Arrange
        var agent = CreateAgent();
        SetupChatResponse("""{"starters":[]}""");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _suggester.SuggestStartersAsync(agent));
    }

    [Fact]
    public async Task SuggestStartersAsync_WithMalformedResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var agent = CreateAgent();
        SetupChatResponse("not json");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(() => _suggester.SuggestStartersAsync(agent));
    }

    [Fact]
    public async Task SuggestStartersAsync_WithBlankAndWhitespaceSuggestions_FiltersThemOut()
    {
        // Arrange
        var agent = CreateAgent();
        SetupChatResponse("""{"starters":["", "  ", "A real suggestion"]}""");

        // Act
        var result = await _suggester.SuggestStartersAsync(agent);

        // Assert
        result.ShouldBe(["A real suggestion"]);
    }

    [Fact]
    public async Task SuggestStartersAsync_NeverPersistsAnything()
    {
        // Arrange — the suggester has no repository/service dependency at all, so there is nothing
        // it could persist through. This test documents that guarantee via the constructor shape.
        var agent = CreateAgent();
        SetupChatResponse("""{"starters":["A suggestion"]}""");

        // Act
        await _suggester.SuggestStartersAsync(agent);

        // Assert — only the chat service was ever touched.
        _chatServiceMock.Verify(
            x => x.GetChatResponseAsync(
                It.IsAny<Action<AIChatBuilder>>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _chatServiceMock.VerifyNoOtherCalls();
    }

    private void SetupChatResponse(string text)
    {
        _chatServiceMock
            .Setup(x => x.GetChatResponseAsync(
                It.IsAny<Action<AIChatBuilder>>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
    }

    private static AIAgent CreateAgent(string? instructions = "Help the user write blog posts.")
    {
        return new AIAgent
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            Config = new AIStandardAgentConfig
            {
                Instructions = instructions
            },
            IsActive = true
        };
    }
}

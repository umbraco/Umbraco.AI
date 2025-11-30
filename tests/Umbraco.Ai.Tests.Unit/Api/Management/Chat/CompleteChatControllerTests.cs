using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Tests.Common.Builders;
using Umbraco.Ai.Web.Api.Management.Chat.Controllers;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Chat;

public class CompleteChatControllerTests
{
    private readonly Mock<IAiChatService> _chatServiceMock;
    private readonly Mock<IAiProfileRepository> _profileRepositoryMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly CompleteChatController _controller;

    public CompleteChatControllerTests()
    {
        _chatServiceMock = new Mock<IAiChatService>();
        _profileRepositoryMock = new Mock<IAiProfileRepository>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new CompleteChatController(
            _chatServiceMock.Object,
            _profileRepositoryMock.Object,
            _mapperMock.Object);
    }

    #region CompleteChat

    [Fact]
    public async Task CompleteChat_WithValidRequest_ReturnsOkWithResponse()
    {
        // Arrange
        var requestModel = new ChatRequestModel
        {
            Messages = new[]
            {
                new ChatMessageModel { Role = "user", Content = "Hello" }
            }
        };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hi there!"));

        var responseModel = new ChatResponseModel
        {
            Message = new ChatMessageModel { Role = "assistant", Content = "Hi there!" }
        };

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        _mapperMock
            .Setup(x => x.Map<ChatResponseModel>(chatResponse))
            .Returns(responseModel);

        // Act
        var result = await _controller.CompleteChat(requestModel);

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<ChatResponseModel>();
        response.Message.Content.ShouldBe("Hi there!");
    }

    [Fact]
    public async Task CompleteChat_WithProfileId_CallsServiceWithProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new ChatRequestModel
        {
            ProfileIdOrAlias = new IdOrAlias(profileId),
            Messages = new[]
            {
                new ChatMessageModel { Role = "user", Content = "Hello" }
            }
        };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hi!"));

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetResponseAsync(
                profileId,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        _mapperMock
            .Setup(x => x.Map<ChatResponseModel>(It.IsAny<ChatResponse>()))
            .Returns(new ChatResponseModel());

        // Act
        await _controller.CompleteChat(requestModel);

        // Assert
        _chatServiceMock.Verify(x => x.GetResponseAsync(
            profileId,
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteChat_WithoutProfileId_CallsServiceWithDefaultProfile()
    {
        // Arrange
        var requestModel = new ChatRequestModel
        {
            Messages = new[]
            {
                new ChatMessageModel { Role = "user", Content = "Hello" }
            }
        };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hi!"));

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatResponse);

        _mapperMock
            .Setup(x => x.Map<ChatResponseModel>(It.IsAny<ChatResponse>()))
            .Returns(new ChatResponseModel());

        // Act
        await _controller.CompleteChat(requestModel);

        // Assert - Should call the overload without profileId
        _chatServiceMock.Verify(x => x.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteChat_WithProfileNotFound_Returns404NotFound()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new ChatRequestModel
        {
            ProfileIdOrAlias = new IdOrAlias(profileId),
            Messages = new[]
            {
                new ChatMessageModel { Role = "user", Content = "Hello" }
            }
        };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetResponseAsync(
                profileId,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Profile with ID '{profileId}' not found"));

        // Act
        var result = await _controller.CompleteChat(requestModel);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
    }

    [Fact]
    public async Task CompleteChat_WithChatCompletionFailure_Returns400BadRequest()
    {
        // Arrange
        var requestModel = new ChatRequestModel
        {
            Messages = new[]
            {
                new ChatMessageModel { Role = "user", Content = "Hello" }
            }
        };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello")
        };

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API quota exceeded"));

        // Act
        var result = await _controller.CompleteChat(requestModel);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        var problemDetails = badRequestResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Chat completion failed");
        problemDetails.Detail.ShouldBe("API quota exceeded");
        problemDetails.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CompleteChat_MapsRequestMessagesToService()
    {
        // Arrange
        var requestModel = new ChatRequestModel
        {
            Messages = new[]
            {
                new ChatMessageModel { Role = "system", Content = "You are helpful" },
                new ChatMessageModel { Role = "user", Content = "Hello" }
            }
        };

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are helpful"),
            new(ChatRole.User, "Hello")
        };

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hi!"));

        IEnumerable<ChatMessage>? capturedMessages = null;
        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>((m, _, _) => capturedMessages = m.ToList())
            .ReturnsAsync(chatResponse);

        _mapperMock
            .Setup(x => x.Map<ChatResponseModel>(It.IsAny<ChatResponse>()))
            .Returns(new ChatResponseModel());

        // Act
        await _controller.CompleteChat(requestModel);

        // Assert
        capturedMessages.ShouldNotBeNull();
        capturedMessages!.Count().ShouldBe(2);
    }

    #endregion
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Services;
using Umbraco.Ai.Web.Api.Management.Chat.Controllers;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Tests.Unit.Api.Management.Chat;

public class StreamChatControllerTests
{
    private readonly Mock<IAiChatService> _chatServiceMock;
    private readonly Mock<IUmbracoMapper> _mapperMock;
    private readonly StreamChatController _controller;
    private readonly DefaultHttpContext _httpContext;

    public StreamChatControllerTests()
    {
        _chatServiceMock = new Mock<IAiChatService>();
        _mapperMock = new Mock<IUmbracoMapper>();

        _controller = new StreamChatController(_chatServiceMock.Object, _mapperMock.Object);

        // Set up HTTP context for SSE response
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    #region Stream

    [Fact]
    public async Task Stream_WithValidRequest_SetsCorrectContentType()
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
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        _mapperMock
            .Setup(x => x.Map<ChatStreamChunkModel>(It.IsAny<ChatResponseUpdate>()))
            .Returns(new ChatStreamChunkModel());

        // Act
        await _controller.Stream(requestModel);

        // Assert
        _httpContext.Response.ContentType.ShouldBe("text/event-stream");
    }

    [Fact]
    public async Task Stream_WithValidRequest_SetsNoCacheHeader()
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
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        // Act
        await _controller.Stream(requestModel);

        // Assert
        _httpContext.Response.Headers.CacheControl.ToString().ShouldBe("no-cache");
    }

    [Fact]
    public async Task Stream_WithProfileId_CallsServiceWithProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new ChatRequestModel
        {
            ProfileId = profileId,
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
            .Setup(x => x.GetStreamingResponseAsync(
                profileId,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        // Act
        await _controller.Stream(requestModel);

        // Assert
        _chatServiceMock.Verify(x => x.GetStreamingResponseAsync(
            profileId,
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Stream_WithoutProfileId_CallsServiceWithDefaultProfile()
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
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<ChatResponseUpdate>());

        // Act
        await _controller.Stream(requestModel);

        // Assert - Should call the overload without profileId
        _chatServiceMock.Verify(x => x.GetStreamingResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Stream_WritesChunksToResponse()
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

        var updates = new List<ChatResponseUpdate>
        {
            new(ChatRole.Assistant, "Hi "),
            new(ChatRole.Assistant, "there!")
        };

        _mapperMock
            .Setup(x => x.MapEnumerable<ChatMessageModel, ChatMessage>(It.IsAny<IEnumerable<ChatMessageModel>>()))
            .Returns(chatMessages);

        _chatServiceMock
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(updates.ToAsyncEnumerable());

        _mapperMock
            .Setup(x => x.Map<ChatStreamChunkModel>(It.IsAny<ChatResponseUpdate>()))
            .Returns(new ChatStreamChunkModel { Content = "chunk" });

        // Act
        await _controller.Stream(requestModel);

        // Assert
        _httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        // Should contain data events and [DONE]
        responseBody.ShouldContain("data:");
        responseBody.ShouldContain("[DONE]");
    }

    [Fact]
    public async Task Stream_WithProfileNotFound_SetsNotFoundStatusCode()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var requestModel = new ChatRequestModel
        {
            ProfileId = profileId,
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
            .Setup(x => x.GetStreamingResponseAsync(
                profileId,
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(ThrowingAsyncEnumerable($"Profile with ID '{profileId}' not found"));

        // Act
        await _controller.Stream(requestModel);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Stream_WithStreamingFailure_SetsBadRequestStatusCode()
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
            .Setup(x => x.GetStreamingResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Returns(ThrowingAsyncEnumerable("Connection timeout", isNotFound: false));

        // Act
        await _controller.Stream(requestModel);

        // Assert
        _httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Helper to create an async enumerable that throws an exception.
    /// </summary>
    private static async IAsyncEnumerable<ChatResponseUpdate> ThrowingAsyncEnumerable(string message, bool isNotFound = true)
    {
        await Task.CompletedTask;

        if (isNotFound)
        {
            throw new InvalidOperationException(message);
        }
        else
        {
            throw new Exception(message);
        }

        // This is never reached but needed for compiler
        yield break;
    }

    #endregion
}

/// <summary>
/// Extension to convert IEnumerable to IAsyncEnumerable for testing.
/// </summary>
public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.CompletedTask;
            yield return item;
        }
    }
}

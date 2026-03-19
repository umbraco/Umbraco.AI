using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.AGUI.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIFileProcessorTests
{
    private readonly Mock<IAGUIFileStore> _mockStore;
    private readonly AGUIFileProcessor _processor;

    public AGUIFileProcessorTests()
    {
        _mockStore = new Mock<IAGUIFileStore>();
        _processor = new AGUIFileProcessor(
            _mockStore.Object,
            NullLogger<AGUIFileProcessor>.Instance);
    }

    [Fact]
    public async Task ProcessInbound_NullMessages_ReturnsEmpty()
    {
        var result = await _processor.ProcessInboundAsync(null, "thread-1");

        result.RewrittenMessages.ShouldBeEmpty();
        result.ResolvedMessages.ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessInbound_TextOnlyMessages_PassesThrough()
    {
        var messages = new List<AGUIMessage>
        {
            new() { Role = AGUIMessageRole.User, Content = "Hello" },
            new() { Role = AGUIMessageRole.Assistant, Content = "Hi" }
        };

        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        result.RewrittenMessages.ShouldBe(messages);
        result.ResolvedMessages.ShouldBe(messages);
        _mockStore.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessInbound_Base64Data_StoresAndRewritesToId()
    {
        // Arrange
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var messages = new List<AGUIMessage>
        {
            new()
            {
                Role = AGUIMessageRole.User,
                Content = "Check this image",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Check this image" },
                    new AGUIBinaryInputContent { MimeType = "image/png", Data = base64, Filename = "test.png" }
                }
            }
        };

        _mockStore
            .Setup(s => s.StoreAsync("thread-1", It.IsAny<byte[]>(), "image/png", "test.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("file-abc");

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — rewritten should have id, no data
        var rewritten = result.RewrittenMessages.First();
        var rewrittenBinary = rewritten.ContentParts![1].ShouldBeOfType<AGUIBinaryInputContent>();
        rewrittenBinary.Id.ShouldBe("file-abc");
        rewrittenBinary.Data.ShouldBeNull();

        // Assert — resolved should have bytes
        var resolved = result.ResolvedMessages.First();
        var resolvedBinary = resolved.ContentParts![1].ShouldBeOfType<AGUIBinaryInputContent>();
        resolvedBinary.Id.ShouldBe("file-abc");
        resolvedBinary.ResolvedData.ShouldNotBeNull();
        resolvedBinary.ResolvedData!.Length.ShouldBe(3);
    }

    [Fact]
    public async Task ProcessInbound_IdReference_ResolvesFromStore()
    {
        // Arrange
        var storedData = new byte[] { 10, 20, 30 };
        var messages = new List<AGUIMessage>
        {
            new()
            {
                Role = AGUIMessageRole.User,
                Content = "Analyze this",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Analyze this" },
                    new AGUIBinaryInputContent { MimeType = "image/png", Id = "file-abc", Filename = "test.png" }
                }
            }
        };

        _mockStore
            .Setup(s => s.ResolveAsync("thread-1", "file-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AGUIStoredFile { Data = storedData, MimeType = "image/png", Filename = "test.png" });

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — rewritten stays the same (already has id)
        var rewrittenBinary = result.RewrittenMessages.First().ContentParts![1].ShouldBeOfType<AGUIBinaryInputContent>();
        rewrittenBinary.Id.ShouldBe("file-abc");

        // Assert — resolved has bytes
        var resolvedBinary = result.ResolvedMessages.First().ContentParts![1].ShouldBeOfType<AGUIBinaryInputContent>();
        resolvedBinary.ResolvedData.ShouldBe(storedData);
    }

    [Fact]
    public async Task ProcessInbound_MixedMessages_OnlyProcessesBinaryParts()
    {
        // Arrange — one message with binary, one without
        var base64 = Convert.ToBase64String(new byte[] { 1 });
        var messages = new List<AGUIMessage>
        {
            new() { Role = AGUIMessageRole.User, Content = "First message" },
            new()
            {
                Role = AGUIMessageRole.User,
                Content = "With image",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "With image" },
                    new AGUIBinaryInputContent { MimeType = "image/png", Data = base64 }
                }
            }
        };

        _mockStore
            .Setup(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("file-xyz");

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — first message passes through, second gets processed
        var rewrittenList = result.RewrittenMessages.ToList();
        rewrittenList[0].Content.ShouldBe("First message");
        rewrittenList[0].ContentParts.ShouldBeNull();

        var processedBinary = rewrittenList[1].ContentParts![1].ShouldBeOfType<AGUIBinaryInputContent>();
        processedBinary.Id.ShouldBe("file-xyz");
    }
}

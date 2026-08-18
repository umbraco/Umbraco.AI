using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.FileStore;
using Umbraco.AI.AGUI.Models;
using Umbraco.Cms.Core.Configuration.Models;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.AGUI;

public class AGUIFileProcessorTests
{
    private readonly Mock<IAIFileStore> _mockStore;
    private readonly ContentSettings _contentSettings;
    private readonly AGUIFileProcessor _processor;

    public AGUIFileProcessorTests()
    {
        _mockStore = new Mock<IAIFileStore>();
        _contentSettings = new ContentSettings();
        var contentSettingsMonitor = Mock.Of<IOptionsMonitor<ContentSettings>>(m => m.CurrentValue == _contentSettings);
        _processor = new AGUIFileProcessor(
            _mockStore.Object,
            contentSettingsMonitor,
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
            new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "Hello" },
            new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.Assistant, Content = "Hi" }
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
                Id = Guid.NewGuid().ToString(),
                Role = AGUIMessageRole.User,
                Content = "Check this image",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Check this image" },
                    new AGUIImageInputContent
                    {
                        Source = new AGUIInputContentDataSource { Value = base64, MimeType = "image/png" },
                        Metadata = new Dictionary<string, object?> { ["filename"] = "test.png" }
                    }
                }
            }
        };

        _mockStore
            .Setup(s => s.StoreAsync("thread-1", It.IsAny<byte[]>(), "image/png", "test.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("file-abc");

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — rewritten should have id in metadata; no inline data unless URL provider absent
        var rewritten = result.RewrittenMessages.First();
        var rewrittenBinary = rewritten.ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        rewrittenBinary.Metadata.ShouldNotBeNull();
        rewrittenBinary.Metadata!["fileId"].ShouldBe("file-abc");

        // Assert — resolved should have bytes attached via metadata
        var resolved = result.ResolvedMessages.First();
        var resolvedBinary = resolved.ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        resolvedBinary.Metadata.ShouldNotBeNull();
        resolvedBinary.Metadata!["fileId"].ShouldBe("file-abc");
        var resolvedBytes = AGUIFileProcessor.GetResolvedBytes(resolvedBinary);
        resolvedBytes.ShouldNotBeNull();
        resolvedBytes!.Length.ShouldBe(3);
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
                Id = Guid.NewGuid().ToString(),
                Role = AGUIMessageRole.User,
                Content = "Analyze this",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Analyze this" },
                    new AGUIImageInputContent
                    {
                        Source = new AGUIInputContentUrlSource { Value = "https://server/file/file-abc", MimeType = "image/png" },
                        Metadata = new Dictionary<string, object?>
                        {
                            ["filename"] = "test.png",
                            ["fileId"] = "file-abc"
                        }
                    }
                }
            }
        };

        _mockStore
            .Setup(s => s.ResolveAsync("thread-1", "file-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIStoredFile { Data = storedData, MimeType = "image/png", Filename = "test.png" });

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — rewritten stays the same (already has id)
        var rewrittenBinary = result.RewrittenMessages.First().ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        rewrittenBinary.Metadata.ShouldNotBeNull();
        rewrittenBinary.Metadata!["fileId"].ShouldBe("file-abc");

        // Assert — resolved has bytes
        var resolvedBinary = result.ResolvedMessages.First().ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        AGUIFileProcessor.GetResolvedBytes(resolvedBinary).ShouldBe(storedData);
    }

    /// <summary>
    /// A follow-up turn's request body is real wire JSON, not an in-memory object graph like the other
    /// tests above — ASP.NET Core model-binds it via <c>System.Text.Json</c>, which deserializes the
    /// untyped <c>Metadata</c> dictionary's <c>fileId</c> value as a <see cref="System.Text.Json.JsonElement"/>,
    /// never the original <see cref="string"/>. A resolver that only matched <c>string</c> would silently
    /// treat every follow-up as an unresolvable external URL instead of looking up the stored bytes —
    /// exactly the bug this test exists to catch (it passed against the in-memory metadata above while
    /// failing for real, on a second turn, in the running app).
    /// </summary>
    [Fact]
    public async Task ProcessInbound_IdReferenceLoadedFromJson_StillResolves()
    {
        // Arrange
        var storedData = new byte[] { 10, 20, 30 };
        AGUIMessage[] messages =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Role = AGUIMessageRole.User,
                Content = "Analyze this",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Analyze this" },
                    new AGUIImageInputContent
                    {
                        Source = new AGUIInputContentUrlSource { Value = "https://server/file/file-abc", MimeType = "image/png" },
                        Metadata = new Dictionary<string, object?>
                        {
                            ["filename"] = "test.png",
                            ["fileId"] = "file-abc"
                        }
                    }
                }
            }
        ];
        var requestJson = System.Text.Json.JsonSerializer.Serialize(messages);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<AGUIMessage[]>(requestJson)!;

        // Confirms the round trip actually reproduces the JsonElement case rather than testing nothing.
        var media = (AGUIImageInputContent)deserialized[0].ContentParts![1];
        media.Metadata!["fileId"].ShouldBeOfType<System.Text.Json.JsonElement>();

        _mockStore
            .Setup(s => s.ResolveAsync("thread-1", "file-abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIStoredFile { Data = storedData, MimeType = "image/png", Filename = "test.png" });

        // Act
        var result = await _processor.ProcessInboundAsync(deserialized, "thread-1");

        // Assert — resolved has bytes
        var resolvedBinary = result.ResolvedMessages.First().ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        AGUIFileProcessor.GetResolvedBytes(resolvedBinary).ShouldBe(storedData);
    }

    [Fact]
    public async Task ProcessInbound_DisallowedExtension_SkipsFileAndReturnsUnchanged()
    {
        // Arrange — config file extension is disallowed
        _contentSettings.DisallowedUploadedFileExtensions.Add("config");

        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var messages = new List<AGUIMessage>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Role = AGUIMessageRole.User,
                Content = "Check this file",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Check this file" },
                    new AGUIDocumentInputContent
                    {
                        Source = new AGUIInputContentDataSource { Value = base64, MimeType = "application/octet-stream" },
                        Metadata = new Dictionary<string, object?> { ["filename"] = "web.config" }
                    }
                }
            }
        };

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — file should not be stored
        _mockStore.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);

        // Assert — binary part returned unchanged (still has data source, no fileId)
        var rewrittenBinary = result.RewrittenMessages.First().ContentParts![1].ShouldBeOfType<AGUIDocumentInputContent>();
        var dataSource = rewrittenBinary.Source.ShouldBeOfType<AGUIInputContentDataSource>();
        dataSource.Value.ShouldBe(base64);
        if (rewrittenBinary.Metadata is not null)
        {
            rewrittenBinary.Metadata.ContainsKey("fileId").ShouldBeFalse();
        }
    }

    [Fact]
    public async Task ProcessInbound_AllowedExtension_StoresFile()
    {
        // Arrange — only allow png
        _contentSettings.AllowedUploadedFileExtensions.Add("png");

        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var messages = new List<AGUIMessage>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Role = AGUIMessageRole.User,
                Content = "Check this image",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "Check this image" },
                    new AGUIImageInputContent
                    {
                        Source = new AGUIInputContentDataSource { Value = base64, MimeType = "image/png" },
                        Metadata = new Dictionary<string, object?> { ["filename"] = "test.png" }
                    }
                }
            }
        };

        _mockStore
            .Setup(s => s.StoreAsync("thread-1", It.IsAny<byte[]>(), "image/png", "test.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("file-abc");

        // Act
        var result = await _processor.ProcessInboundAsync(messages, "thread-1");

        // Assert — file should be stored
        var rewrittenBinary = result.RewrittenMessages.First().ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        rewrittenBinary.Metadata.ShouldNotBeNull();
        rewrittenBinary.Metadata!["fileId"].ShouldBe("file-abc");
    }

    [Fact]
    public async Task ProcessInbound_MixedMessages_OnlyProcessesBinaryParts()
    {
        // Arrange — one message with binary, one without
        var base64 = Convert.ToBase64String(new byte[] { 1 });
        var messages = new List<AGUIMessage>
        {
            new() { Id = Guid.NewGuid().ToString(), Role = AGUIMessageRole.User, Content = "First message" },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Role = AGUIMessageRole.User,
                Content = "With image",
                ContentParts = new List<AGUIInputContent>
                {
                    new AGUITextInputContent { Text = "With image" },
                    new AGUIImageInputContent
                    {
                        Source = new AGUIInputContentDataSource { Value = base64, MimeType = "image/png" }
                    }
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

        var processedBinary = rewrittenList[1].ContentParts![1].ShouldBeOfType<AGUIImageInputContent>();
        processedBinary.Metadata.ShouldNotBeNull();
        processedBinary.Metadata!["fileId"].ShouldBe("file-xyz");
    }
}

using System.Text.Json;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.EntityAdapter.Adapters;
using Umbraco.AI.Core.FileProcessing;
using Umbraco.AI.Core.Media;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Tests.Unit.EntityAdapter.Adapters;

public class MediaEntityAdapterTests
{
    private readonly Mock<IMediaTypeService> _mediaTypeServiceMock = new();
    private readonly Mock<IPublishedContentTypeCache> _typeCacheMock = new();
    private readonly Mock<IPropertyEditorSchemaService> _schemaServiceMock = new();
    private readonly Mock<IAIUmbracoMediaResolver> _mediaResolverMock = new();

    private MediaEntityAdapter CreateAdapter(params IAIFileProcessingHandler[] handlers)
    {
        var collection = new AIFileProcessingHandlerCollection(() => handlers);
        return new MediaEntityAdapter(
            _mediaTypeServiceMock.Object,
            _typeCacheMock.Object,
            _schemaServiceMock.Object,
            _mediaResolverMock.Object,
            collection);
    }

    private static AISerializedEntity CreateEntity(string name = "report.csv")
        => new()
        {
            EntityType = "media",
            Unique = "11111111-1111-1111-1111-111111111111",
            Name = name,
            Data = JsonDocument.Parse("{}").RootElement,
        };

    [Fact]
    public async Task FormatForLlmAsync_WithTextExtractableMedia_AppendsExtractedContent()
    {
        // Arrange
        var entity = CreateEntity();
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = "a,b\n1,2"u8.ToArray(), MediaType = "text/csv" });

        var handler = new FakeHandler("text/csv", "a,b\n1,2");
        var adapter = CreateAdapter(handler);

        // Act
        var result = await adapter.FormatForLlmAsync(entity);

        // Assert
        result.ShouldContain("### File Content"); // extracted text is delimited from the metadata
        result.ShouldContain("a,b\n1,2");
        result.ShouldContain(entity.Unique); // still includes the existing metadata line
    }

    [Fact]
    public async Task FormatForLlmAsync_CalledThroughInterfaceType_StillAppendsExtractedContent()
    {
        // Arrange — production (AIEntityContextHelper) resolves adapters as IAIEntityAdapter via
        // AIEntityAdapterCollection.GetAdapter, then calls FormatForLlmAsync through that interface
        // reference. IAIEntityAdapter.FormatForLlmAsync has a default interface implementation, so
        // an interface-typed call only reaches this adapter's implementation because
        // AIEntityAdapterBase declares a virtual FormatForLlmAsync that MediaEntityAdapter
        // overrides. If that virtual member were removed, the call here would silently resolve to
        // the interface default (metadata-only) even though the identical call on the concrete type
        // above works. This test exercises the actual production call shape to guard against that.
        var entity = CreateEntity();
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = "a,b\n1,2"u8.ToArray(), MediaType = "text/csv" });

        var handler = new FakeHandler("text/csv", "a,b\n1,2");
        IAIEntityAdapter adapter = CreateAdapter(handler);

        // Act
        var result = await adapter.FormatForLlmAsync(entity);

        // Assert
        result.ShouldContain("a,b\n1,2");
    }

    [Fact]
    public async Task FormatForLlmAsync_WhenMediaCannotBeResolved_FallsBackToMetadataOnly()
    {
        // Arrange — a handler claims text/csv, so the resolve is attempted, but it yields nothing
        var entity = CreateEntity();
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIMediaContent?)null);

        var adapter = CreateAdapter(new FakeHandler("text/csv", "a,b\n1,2"));

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
    }

    [Fact]
    public async Task FormatForLlmAsync_WhenNoHandlerMatchesMediaType_FallsBackToMetadataOnlyWithoutResolving()
    {
        // Arrange — e.g. a real image, which has no text-extraction handler. The handler lookup is
        // driven off the filename extension, so the expensive resolve must never happen at all.
        var entity = CreateEntity(name: "photo.png");
        var adapter = CreateAdapter(); // no handlers registered

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FormatForLlmAsync_WithAudioMedia_NeverResolvesOrConsultsHandlers()
    {
        // Arrange — audio transcription is a paid, per-turn side effect. This always-on
        // "currently open entity" context path must never trigger it, so an audio file is
        // rejected on extension alone: no resolve, no handler check.
        var entity = CreateEntity(name: "interview.mp3");
        var handler = new RecordingHandler();
        var adapter = CreateAdapter(handler);

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
        handler.CanHandleCalled.ShouldBeFalse();
        handler.ProcessCalled.ShouldBeFalse();
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FormatForLlmAsync_WithUnrecognizedExtension_NeverResolves()
    {
        // Arrange — nothing in the extension table, so there is nothing a handler could claim
        var entity = CreateEntity(name: "archive.zip");
        var adapter = CreateAdapter(new FakeHandler("application/zip", "stuff"));

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);

        // Assert
        asyncResult.ShouldBe(adapter.FormatForLlm(entity));
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void FormatForLlm_SyncPath_DoesNotTouchTheMediaResolver()
    {
        // Arrange — the original sync method must remain exactly as before: no file I/O.
        var entity = CreateEntity();
        var adapter = CreateAdapter();

        // Act
        adapter.FormatForLlm(entity);

        // Assert
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FakeHandler : IAIFileProcessingHandler
    {
        private readonly string _mimeType;
        private readonly string _content;

        public FakeHandler(string mimeType, string content)
        {
            _mimeType = mimeType;
            _content = content;
        }

        public Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(mimeType, _mimeType, StringComparison.OrdinalIgnoreCase));

        public Task<AIFileProcessingResult> ProcessAsync(
            ReadOnlyMemory<byte> data, string mimeType, string? filename,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new AIFileProcessingResult(_content, false));
    }

    /// <summary>
    /// A handler that claims everything and records whether it was consulted at all — stands in for
    /// <c>AudioTranscriptionFileProcessingHandler</c>, whose <c>CanHandleAsync</c> alone is enough
    /// to commit to a paid transcription call.
    /// </summary>
    private sealed class RecordingHandler : IAIFileProcessingHandler
    {
        public bool CanHandleCalled { get; private set; }

        public bool ProcessCalled { get; private set; }

        public Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
        {
            CanHandleCalled = true;
            return Task.FromResult(true);
        }

        public Task<AIFileProcessingResult> ProcessAsync(
            ReadOnlyMemory<byte> data, string mimeType, string? filename,
            CancellationToken cancellationToken = default)
        {
            ProcessCalled = true;
            return Task.FromResult(new AIFileProcessingResult("transcript", false));
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
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
            collection,
            NullLogger<MediaEntityAdapter>.Instance);
    }

    /// <summary>
    /// Stubs <see cref="IAIUmbracoMediaResolver.GetMediaType"/> — the gate
    /// <see cref="MediaEntityAdapter.FormatForLlm"/> now consults instead of deriving a MIME
    /// type from <see cref="AISerializedEntity.Name"/> — to return the given MIME type (or
    /// <c>null</c> to simulate an unrecognized/unresolvable file).
    /// </summary>
    private void SetupMediaType(string? mediaType)
        => _mediaResolverMock
            .Setup(m => m.GetMediaType(It.IsAny<object>()))
            .Returns(mediaType);

    private static AISerializedEntity CreateEntity(string name = "report.csv")
        => new()
        {
            EntityType = "media",
            Unique = "11111111-1111-1111-1111-111111111111",
            Name = name,
            Data = JsonDocument.Parse("{}").RootElement,
        };

    [Fact]
    public void FormatForLlm_WithTextExtractableMedia_AppendsExtractedContent()
    {
        // Arrange
        var entity = CreateEntity();
        SetupMediaType("text/csv");
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = "a,b\n1,2"u8.ToArray(), MediaType = "text/csv" });

        var handler = new FakeHandler("text/csv", "a,b\n1,2");
        var adapter = CreateAdapter(handler);

        // Act
        var result = adapter.FormatForLlm(entity);

        // Assert
        result.ShouldContain("### File Content"); // extracted text is delimited from the metadata
        result.ShouldContain("a,b\n1,2");
        result.ShouldContain(entity.Unique); // still includes the existing metadata line
    }

    [Fact]
    public void FormatForLlm_CalledThroughInterfaceType_StillAppendsExtractedContent()
    {
        // Arrange — production (AIEntityContextHelper) resolves adapters as IAIEntityAdapter via
        // AIEntityAdapterCollection.GetAdapter, then calls FormatForLlm through that interface
        // reference. This exercises the actual production call shape.
        var entity = CreateEntity();
        SetupMediaType("text/csv");
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = "a,b\n1,2"u8.ToArray(), MediaType = "text/csv" });

        var handler = new FakeHandler("text/csv", "a,b\n1,2");
        IAIEntityAdapter adapter = CreateAdapter(handler);

        // Act
        var result = adapter.FormatForLlm(entity);

        // Assert
        result.ShouldContain("a,b\n1,2");
    }

    [Fact]
    public void FormatForLlm_WhenMediaCannotBeResolved_FallsBackToMetadataOnly()
    {
        // Arrange — a handler claims text/csv, so the resolve is attempted, but it yields nothing
        var entity = CreateEntity();
        SetupMediaType("text/csv");
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIMediaContent?)null);

        var adapter = CreateAdapter(new FakeHandler("text/csv", "a,b\n1,2"));

        // Act
        var result = adapter.FormatForLlm(entity);

        // Assert
        result.ShouldNotContain("### File Content");
    }

    [Fact]
    public void FormatForLlm_WhenNoHandlerMatchesMediaType_FallsBackToMetadataOnlyWithoutResolving()
    {
        // Arrange — e.g. a real image, which has no text-extraction handler. The handler lookup is
        // driven off the file's real MIME type, so the expensive resolve must never happen at all.
        var entity = CreateEntity(name: "photo.png");
        SetupMediaType("image/png");
        var adapter = CreateAdapter(); // no handlers registered

        // Act
        var result = adapter.FormatForLlm(entity);

        // Assert
        result.ShouldNotContain("### File Content");
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void FormatForLlm_WithAudioMedia_NeverResolvesOrConsultsHandlers()
    {
        // Arrange — audio transcription is a paid, per-turn side effect. This always-on
        // "currently open entity" context path must never trigger it, so an audio file is
        // rejected on its real MIME type alone: no resolve, no handler check.
        var entity = CreateEntity(name: "interview.mp3");
        SetupMediaType("audio/mpeg");
        var handler = new RecordingHandler();
        var adapter = CreateAdapter(handler);

        // Act
        adapter.FormatForLlm(entity);

        // Assert
        handler.CanHandleCalled.ShouldBeFalse();
        handler.ProcessCalled.ShouldBeFalse();
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void FormatForLlm_WithUnrecognizedExtension_NeverResolves()
    {
        // Arrange — nothing in the extension table, so there is nothing a handler could claim
        var entity = CreateEntity(name: "archive.zip");
        SetupMediaType(null);
        var adapter = CreateAdapter(new FakeHandler("application/zip", "stuff"));

        // Act
        adapter.FormatForLlm(entity);

        // Assert
        _mediaResolverMock.Verify(
            m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void FormatForLlm_WhenHandlerThrows_FallsBackToMetadataOnly()
    {
        // Arrange — e.g. a corrupted .docx that makes the underlying parser throw. This path is
        // always-on (repeats every turn while the media stays the active context), so a bad file
        // must degrade to metadata-only instead of failing the whole chat request.
        var entity = CreateEntity();
        SetupMediaType("text/csv");
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = "a,b\n1,2"u8.ToArray(), MediaType = "text/csv" });

        var handler = new ThrowingHandler("text/csv");
        var adapter = CreateAdapter(handler);

        // Act
        var result = adapter.FormatForLlm(entity);

        // Assert
        result.ShouldNotContain("### File Content");
    }

    [Fact]
    public void FormatForLlm_WithNoRecognizedMediaType_DoesNotResolve()
    {
        // Arrange — the baseline (metadata-only) case must remain reachable with no I/O at all
        // beyond the sync MIME-type lookup.
        var entity = CreateEntity();
        SetupMediaType(null);
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
    /// A handler that claims a MIME type but throws when asked to process it — stands in for a
    /// corrupted/malformed file (e.g. a damaged .docx) that makes the real parser throw.
    /// </summary>
    private sealed class ThrowingHandler : IAIFileProcessingHandler
    {
        private readonly string _mimeType;

        public ThrowingHandler(string mimeType)
        {
            _mimeType = mimeType;
        }

        public Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(mimeType, _mimeType, StringComparison.OrdinalIgnoreCase));

        public Task<AIFileProcessingResult> ProcessAsync(
            ReadOnlyMemory<byte> data, string mimeType, string? filename,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated corrupted file parse failure.");
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

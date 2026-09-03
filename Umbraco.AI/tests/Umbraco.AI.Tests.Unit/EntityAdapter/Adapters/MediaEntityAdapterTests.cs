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
        result.ShouldContain("a,b\n1,2");
        result.ShouldContain(entity.Unique); // still includes the existing metadata line
    }

    [Fact]
    public async Task FormatForLlmAsync_CalledThroughInterfaceType_StillAppendsExtractedContent()
    {
        // Arrange — production (AIEntityContextHelper) resolves adapters as IAIEntityAdapter via
        // AIEntityAdapterCollection.GetAdapter, then calls FormatForLlmAsync through that interface
        // reference. MediaEntityAdapter.FormatForLlmAsync is a plain (non-override) method because
        // AIEntityAdapterBase never declares a virtual member of that name — it relies on the
        // interface's default implementation. Without MediaEntityAdapter also redeclaring
        // IAIEntityAdapter, an interface-typed call here would silently resolve to that default
        // implementation (metadata-only) instead of this class's override, even though the identical
        // call on the concrete type above works. This test exercises the actual production call
        // shape to guard against that regression.
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
        // Arrange
        var entity = CreateEntity();
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIMediaContent?)null);

        var adapter = CreateAdapter();

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
    }

    [Fact]
    public async Task FormatForLlmAsync_WhenNoHandlerMatchesMediaType_FallsBackToMetadataOnly()
    {
        // Arrange — e.g. a real image, which has no text-extraction handler
        var entity = CreateEntity(name: "photo.png");
        _mediaResolverMock
            .Setup(m => m.ResolveAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = [1, 2, 3], MediaType = "image/png" });

        var adapter = CreateAdapter(); // no handlers registered

        // Act
        var asyncResult = await adapter.FormatForLlmAsync(entity);
        var syncResult = adapter.FormatForLlm(entity);

        // Assert
        asyncResult.ShouldBe(syncResult);
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
}

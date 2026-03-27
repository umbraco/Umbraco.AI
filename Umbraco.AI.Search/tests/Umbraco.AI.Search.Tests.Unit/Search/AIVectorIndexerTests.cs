using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core.Embeddings;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Search.Core.Chunking;
using Umbraco.AI.Search.Core.Configuration;
using Umbraco.AI.Search.Core.Search;
using Umbraco.AI.Search.Core.VectorStore;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Xunit;

namespace Umbraco.AI.Search.Tests.Unit.Search;

public class AIVectorIndexerTests
{
    private const string IndexAlias = "test-index";

    private readonly InMemoryAIVectorStore _store = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly Mock<IAIEmbeddingService> _embeddingServiceMock = new();
    private readonly Mock<IAITextChunker> _chunkerMock = new();
    private readonly AIVectorIndexer _indexer;

    public AIVectorIndexerTests()
    {
        var options = Options.Create(new AIVectorSearchOptions
        {
            ChunkSize = 512,
            ChunkOverlap = 50,
        });

        // Default: chunker returns a single chunk with the input text
        _chunkerMock
            .Setup(c => c.ChunkText(It.IsAny<string>(), It.IsAny<AITextChunkingOptions>()))
            .Returns((string text, AITextChunkingOptions _) =>
                new List<AITextChunk> { new(text, 0, 0, text.Length) });

        // Default: embedding service returns a dummy vector
        _embeddingServiceMock
            .Setup(e => e.GenerateEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> values, EmbeddingGenerationOptions? _, CancellationToken _) =>
            {
                var embeddings = values.Select(_ => new Embedding<float>(new float[] { 1.0f, 0.0f })).ToList();
                return new GeneratedEmbeddings<Embedding<float>>(embeddings);
            });

        // Default: embedding profile is configured
        _profileServiceMock
            .Setup(p => p.HasDefaultProfileAsync(AICapability.Embedding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _indexer = new AIVectorIndexer(
            _store,
            _profileServiceMock.Object,
            _embeddingServiceMock.Object,
            _chunkerMock.Object,
            options,
            NullLogger<AIVectorIndexer>.Instance);
    }

    [Fact]
    public async Task AddOrUpdateAsync_InvariantToVariant_RemovesOldInvariantEntries()
    {
        var docId = Guid.NewGuid();

        // Index as invariant (no culture)
        await _indexer.AddOrUpdateAsync(
            IndexAlias, docId, UmbracoObjectTypes.Document,
            [new Variation(null, null)],
            [CreateField("title", "Hello World", culture: null)],
            null);

        (await _store.GetDocumentCountAsync(IndexAlias)).ShouldBe(1);

        // Re-index as variant (with cultures)
        await _indexer.AddOrUpdateAsync(
            IndexAlias, docId, UmbracoObjectTypes.Document,
            [new Variation("en", null), new Variation("da", null)],
            [
                CreateField("title", "Hello World", culture: "en"),
                CreateField("title", "Hej Verden", culture: "da"),
            ],
            null);

        // Should have 2 entries (en + da), not 3 (old invariant should be gone)
        (await _store.GetDocumentCountAsync(IndexAlias)).ShouldBe(2);

        // Verify no invariant entries remain — get all entries and check cultures
        var allEntries = await _store.GetVectorsByDocumentAsync(IndexAlias, docId.ToString("D"));
        allEntries.ShouldAllBe(e => e.Culture != null);
        allEntries.Select(e => e.Culture).ShouldBe(["da", "en"], ignoreOrder: true);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ReIndexSameCulture_ReplacesEntries()
    {
        var docId = Guid.NewGuid();

        await _indexer.AddOrUpdateAsync(
            IndexAlias, docId, UmbracoObjectTypes.Document,
            [new Variation(null, null)],
            [CreateField("title", "Original text", culture: null)],
            null);

        (await _store.GetDocumentCountAsync(IndexAlias)).ShouldBe(1);

        // Re-index same document with different text
        await _indexer.AddOrUpdateAsync(
            IndexAlias, docId, UmbracoObjectTypes.Document,
            [new Variation(null, null)],
            [CreateField("title", "Updated text", culture: null)],
            null);

        // Should still be 1 entry, not 2
        (await _store.GetDocumentCountAsync(IndexAlias)).ShouldBe(1);
    }

    [Fact]
    public async Task AddOrUpdateAsync_VariantContent_CreatesPerCultureEntries()
    {
        var docId = Guid.NewGuid();

        await _indexer.AddOrUpdateAsync(
            IndexAlias, docId, UmbracoObjectTypes.Document,
            [new Variation("en", null), new Variation("da", null)],
            [
                CreateField("title", "English title", culture: "en"),
                CreateField("title", "Dansk titel", culture: "da"),
            ],
            null);

        (await _store.GetDocumentCountAsync(IndexAlias)).ShouldBe(2);

        var enEntries = await _store.GetVectorsByDocumentAsync(IndexAlias, docId.ToString("D"), "en");
        enEntries.Count.ShouldBe(1);

        var daEntries = await _store.GetVectorsByDocumentAsync(IndexAlias, docId.ToString("D"), "da");
        daEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AddOrUpdateAsync_WithProtection_StoresAccessIds()
    {
        var docId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var protection = new ContentProtection([groupId]);

        await _indexer.AddOrUpdateAsync(
            IndexAlias, docId, UmbracoObjectTypes.Document,
            [new Variation(null, null)],
            [CreateField("title", "Protected content", culture: null)],
            protection);

        var entries = await _store.GetVectorsByDocumentAsync(IndexAlias, docId.ToString("D"));
        entries.Count.ShouldBe(1);
        entries[0].Metadata.ShouldNotBeNull();
        entries[0].Metadata!["accessIds"].ShouldBe(groupId.ToString());
    }

    private static IndexField CreateField(string name, string text, string? culture)
        => new(name, new IndexValue { Texts = [text] }, culture, null);
}

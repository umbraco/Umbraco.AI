using Shouldly;
using Umbraco.AI.Search.Core.VectorStore;
using Xunit;

namespace Umbraco.AI.Search.Tests.Unit.VectorStore;

public class InMemoryAIVectorStoreTests
{
    private readonly InMemoryAIVectorStore _store = new();
    private const string IndexName = "test-index";

    [Fact]
    public async Task UpsertAsync_StoresVector_GetDocumentCountReturnsOne()
    {
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 1.0f, 0.0f, 0.0f });

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task UpsertAsync_SameChunk_OverwritesPrevious()
    {
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 1.0f, 0.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 0.0f, 1.0f, 0.0f });

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task UpsertAsync_MultipleChunks_StoresAll()
    {
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc1", null, 1, new float[] { 0.0f, 1.0f });
        await _store.UpsertAsync(IndexName, "doc1", null, 2, new float[] { 0.5f, 0.5f });

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(3);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAllChunksForDocumentAndCulture()
    {
        await _store.UpsertAsync(IndexName, "doc1", "en", 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc1", "en", 1, new float[] { 0.0f, 1.0f });
        await _store.UpsertAsync(IndexName, "doc1", "da", 0, new float[] { 0.5f, 0.5f });

        await _store.DeleteAsync(IndexName, "doc1", "en");

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(1); // Only da chunk 0 remains
    }

    [Fact]
    public async Task DeleteDocumentAsync_RemovesAllCultures()
    {
        await _store.UpsertAsync(IndexName, "doc1", "en", 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc1", "da", 0, new float[] { 0.0f, 1.0f });
        await _store.UpsertAsync(IndexName, "doc2", null, 0, new float[] { 0.5f, 0.5f });

        await _store.DeleteDocumentAsync(IndexName, "doc1");

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(1); // Only doc2 remains
    }

    [Fact]
    public async Task SearchAsync_ReturnsResultsOrderedBySimilarity()
    {
        await _store.UpsertAsync(IndexName, "doc-x", null, 0, new float[] { 1.0f, 0.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc-y", null, 0, new float[] { 0.0f, 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc-xy", null, 0, new float[] { 0.7f, 0.7f, 0.0f });

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 0.9f, 0.1f, 0.0f }, topK: 3);

        results.Count.ShouldBe(3);
        results[0].DocumentId.ShouldBe("doc-x");
        results[0].Score.ShouldBeGreaterThan(results[1].Score);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCulture()
    {
        await _store.UpsertAsync(IndexName, "doc1", "en", 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc1", "da", 0, new float[] { 0.0f, 1.0f });
        await _store.UpsertAsync(IndexName, "doc2", "en", 0, new float[] { 0.5f, 0.5f });

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f }, culture: "en", topK: 10);

        results.Count.ShouldBe(2); // doc1 en + doc2 en
        results.ShouldAllBe(r => r.DocumentId == "doc1" || r.DocumentId == "doc2");
    }

    [Fact]
    public async Task SearchAsync_NoCultureFilter_ReturnsAll()
    {
        await _store.UpsertAsync(IndexName, "doc1", "en", 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc1", "da", 0, new float[] { 0.0f, 1.0f });

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f }, topK: 10);

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchAsync_EmptyIndex_ReturnsEmpty()
    {
        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f });
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_TopKLimitsResults()
    {
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc2", null, 0, new float[] { 0.0f, 1.0f });
        await _store.UpsertAsync(IndexName, "doc3", null, 0, new float[] { 0.5f, 0.5f });

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f }, topK: 2);
        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchAsync_IncludesMetadata()
    {
        var metadata = new Dictionary<string, object> { ["type"] = "article" };
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 1.0f, 0.0f }, metadata);

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f });

        results[0].Metadata.ShouldNotBeNull();
        results[0].Metadata!["type"].ShouldBe("article");
    }

    [Fact]
    public async Task ResetAsync_ClearsAllDocuments()
    {
        await _store.UpsertAsync(IndexName, "doc1", null, 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc2", "en", 0, new float[] { 0.0f, 1.0f });

        await _store.ResetAsync(IndexName);

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetDocumentCountAsync_NonExistentIndex_ReturnsZero()
    {
        var count = await _store.GetDocumentCountAsync("nonexistent");
        count.ShouldBe(0);
    }

    [Fact]
    public async Task MultipleIndexes_AreIsolated()
    {
        await _store.UpsertAsync("index-a", "doc1", null, 0, new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync("index-b", "doc1", null, 0, new float[] { 0.0f, 1.0f });

        (await _store.GetDocumentCountAsync("index-a")).ShouldBe(1);
        (await _store.GetDocumentCountAsync("index-b")).ShouldBe(1);

        await _store.ResetAsync("index-a");

        (await _store.GetDocumentCountAsync("index-a")).ShouldBe(0);
        (await _store.GetDocumentCountAsync("index-b")).ShouldBe(1);
    }
}

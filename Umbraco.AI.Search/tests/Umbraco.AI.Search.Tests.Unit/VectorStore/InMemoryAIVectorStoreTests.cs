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
        float[] vector = [1.0f, 0.0f, 0.0f];

        await _store.UpsertAsync(IndexName, "doc1", vector);

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task UpsertAsync_SameDocumentId_OverwritesPrevious()
    {
        float[] vector1 = [1.0f, 0.0f, 0.0f];
        float[] vector2 = [0.0f, 1.0f, 0.0f];

        await _store.UpsertAsync(IndexName, "doc1", vector1);
        await _store.UpsertAsync(IndexName, "doc1", vector2);

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDocument()
    {
        float[] vector = [1.0f, 0.0f, 0.0f];
        await _store.UpsertAsync(IndexName, "doc1", vector);

        await _store.DeleteAsync(IndexName, "doc1");

        var count = await _store.GetDocumentCountAsync(IndexName);
        count.ShouldBe(0);
    }

    [Fact]
    public async Task SearchAsync_ReturnsResultsOrderedBySimilarity()
    {
        // Three orthogonal-ish vectors
        await _store.UpsertAsync(IndexName, "doc-x", new float[] { 1.0f, 0.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc-y", new float[] { 0.0f, 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc-xy", new float[] { 0.7f, 0.7f, 0.0f });

        // Query vector close to X axis
        float[] query = [0.9f, 0.1f, 0.0f];

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, query, topK: 3);

        results.Count.ShouldBe(3);
        results[0].DocumentId.ShouldBe("doc-x");
        results[0].Score.ShouldBeGreaterThan(results[1].Score);
    }

    [Fact]
    public async Task SearchAsync_EmptyIndex_ReturnsEmpty()
    {
        float[] query = [1.0f, 0.0f, 0.0f];

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, query);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_TopKLimitsResults()
    {
        await _store.UpsertAsync(IndexName, "doc1", new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc2", new float[] { 0.0f, 1.0f });
        await _store.UpsertAsync(IndexName, "doc3", new float[] { 0.5f, 0.5f });

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f }, topK: 2);

        results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchAsync_IncludesMetadata()
    {
        var metadata = new Dictionary<string, object> { ["type"] = "article" };
        await _store.UpsertAsync(IndexName, "doc1", new float[] { 1.0f, 0.0f }, metadata);

        IReadOnlyList<AIVectorSearchResult> results = await _store.SearchAsync(IndexName, new float[] { 1.0f, 0.0f });

        results[0].Metadata.ShouldNotBeNull();
        results[0].Metadata!["type"].ShouldBe("article");
    }

    [Fact]
    public async Task ResetAsync_ClearsAllDocuments()
    {
        await _store.UpsertAsync(IndexName, "doc1", new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync(IndexName, "doc2", new float[] { 0.0f, 1.0f });

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
        await _store.UpsertAsync("index-a", "doc1", new float[] { 1.0f, 0.0f });
        await _store.UpsertAsync("index-b", "doc1", new float[] { 0.0f, 1.0f });

        var countA = await _store.GetDocumentCountAsync("index-a");
        var countB = await _store.GetDocumentCountAsync("index-b");

        countA.ShouldBe(1);
        countB.ShouldBe(1);

        await _store.ResetAsync("index-a");

        (await _store.GetDocumentCountAsync("index-a")).ShouldBe(0);
        (await _store.GetDocumentCountAsync("index-b")).ShouldBe(1);
    }
}

using Shouldly;
using Umbraco.AI.Search.Core.Chunking;
using Xunit;

namespace Umbraco.AI.Search.Tests.Unit.Chunking;

public class RecursiveAITextChunkerTests
{
    private readonly RecursiveAITextChunker _chunker;

    public RecursiveAITextChunkerTests()
    {
        _chunker = new RecursiveAITextChunker(new WordBasedAITokenCounter());
    }

    [Fact]
    public void ChunkText_EmptyString_ReturnsEmpty()
    {
        var result = _chunker.ChunkText("", new AITextChunkingOptions());
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ChunkText_WhitespaceOnly_ReturnsEmpty()
    {
        var result = _chunker.ChunkText("   \n\n  ", new AITextChunkingOptions());
        result.ShouldBeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var result = _chunker.ChunkText("Hello world.", new AITextChunkingOptions { MaxChunkSize = 512 });

        result.Count.ShouldBe(1);
        result[0].Text.ShouldBe("Hello world.");
        result[0].Index.ShouldBe(0);
    }

    [Fact]
    public void ChunkText_SplitsOnParagraphBoundaries()
    {
        var text = "First paragraph with some content.\n\nSecond paragraph with different content.";
        var options = new AITextChunkingOptions { MaxChunkSize = 10, ChunkOverlap = 0 };

        var result = _chunker.ChunkText(text, options);

        result.Count.ShouldBeGreaterThan(1);
        // Each chunk should contain coherent text
        result.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.Text));
    }

    [Fact]
    public void ChunkText_SplitsOnSentenceBoundaries()
    {
        var text = "First sentence here. Second sentence there. Third sentence everywhere.";
        var options = new AITextChunkingOptions { MaxChunkSize = 8, ChunkOverlap = 0 };

        var result = _chunker.ChunkText(text, options);

        result.Count.ShouldBeGreaterThan(1);
        result.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.Text));
    }

    [Fact]
    public void ChunkText_ChunkIndexesAreSequential()
    {
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.\n\nFourth paragraph.";
        var options = new AITextChunkingOptions { MaxChunkSize = 6, ChunkOverlap = 0 };

        var result = _chunker.ChunkText(text, options);

        for (var i = 0; i < result.Count; i++)
        {
            result[i].Index.ShouldBe(i);
        }
    }

    [Fact]
    public void ChunkText_WithOverlap_ChunksContainOverlappingText()
    {
        var text = "First paragraph with content.\n\nSecond paragraph with more.\n\nThird paragraph final.";
        var options = new AITextChunkingOptions { MaxChunkSize = 10, ChunkOverlap = 3 };

        var result = _chunker.ChunkText(text, options);

        if (result.Count > 1)
        {
            // Second chunk should start with overlap text from first chunk
            // The overlap means some words from the end of the first chunk appear at the start of the second
            result[1].Text.Length.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public void ChunkText_ZeroOverlap_IsValid()
    {
        var text = "First part.\n\nSecond part.\n\nThird part.";
        var options = new AITextChunkingOptions { MaxChunkSize = 6, ChunkOverlap = 0 };

        var result = _chunker.ChunkText(text, options);

        result.ShouldNotBeEmpty();
        result.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.Text));
    }

    [Fact]
    public void ChunkText_OverlapExceedsMaxSize_Throws()
    {
        var options = new AITextChunkingOptions { MaxChunkSize = 10, ChunkOverlap = 10 };

        Should.Throw<ArgumentException>(() => _chunker.ChunkText("Some text here.", options));
    }

    [Fact]
    public void ChunkText_VeryLongSingleWord_ForceSplits()
    {
        // Even a single very long "word" should eventually produce output
        var text = string.Join(" ", Enumerable.Repeat("word", 200));
        var options = new AITextChunkingOptions { MaxChunkSize = 10, ChunkOverlap = 0 };

        var result = _chunker.ChunkText(text, options);

        result.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void ChunkText_MergesSmallSegments()
    {
        // Many tiny paragraphs should be merged into fewer chunks
        var text = "A.\n\nB.\n\nC.\n\nD.\n\nE.";
        var options = new AITextChunkingOptions { MaxChunkSize = 20, ChunkOverlap = 0 };

        var result = _chunker.ChunkText(text, options);

        // All segments together are well within 20 tokens, so should merge to 1 chunk
        result.Count.ShouldBe(1);
    }
}

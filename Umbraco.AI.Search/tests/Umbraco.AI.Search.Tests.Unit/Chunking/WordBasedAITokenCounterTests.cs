using Shouldly;
using Umbraco.AI.Search.Core.Chunking;
using Xunit;

namespace Umbraco.AI.Search.Tests.Unit.Chunking;

public class WordBasedAITokenCounterTests
{
    private readonly WordBasedAITokenCounter _counter = new();

    [Fact]
    public void CountTokens_EmptyString_ReturnsZero()
    {
        _counter.CountTokens("").ShouldBe(0);
    }

    [Fact]
    public void CountTokens_WhitespaceOnly_ReturnsZero()
    {
        _counter.CountTokens("   \t\n  ").ShouldBe(0);
    }

    [Fact]
    public void CountTokens_SingleWord_ReturnsTwo()
    {
        // 1 word * 1.3 = 1.3, ceiling = 2
        _counter.CountTokens("hello").ShouldBe(2);
    }

    [Fact]
    public void CountTokens_MultipleWords_AppliesMultiplier()
    {
        // 10 words * 1.3 = 13
        _counter.CountTokens("one two three four five six seven eight nine ten").ShouldBe(13);
    }

    [Fact]
    public void CountTokens_ExtraWhitespace_CountsWordsCorrectly()
    {
        // Should count 3 words despite irregular spacing
        var result = _counter.CountTokens("  hello   world   test  ");
        result.ShouldBe(4); // 3 * 1.3 = 3.9, ceiling = 4
    }
}

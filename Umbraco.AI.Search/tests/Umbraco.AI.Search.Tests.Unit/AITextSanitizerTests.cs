using Shouldly;
using Umbraco.AI.Search.Core.Utils;
using Xunit;

namespace Umbraco.AI.Search.Tests.Unit;

public class AITextSanitizerTests
{
    [Fact]
    public void StripHtml_RemovesTags()
    {
        var result = AITextSanitizer.StripHtml("<p>Hello <strong>world</strong></p>");
        result.ShouldBe("Hello world");
    }

    [Fact]
    public void StripHtml_DecodesEntities()
    {
        var result = AITextSanitizer.StripHtml("Tom &amp; Jerry &lt;3");
        result.ShouldBe("Tom & Jerry <3");
    }

    [Fact]
    public void StripHtml_CollapsesWhitespace()
    {
        var result = AITextSanitizer.StripHtml("Hello   \n\n  world   ");
        result.ShouldBe("Hello world");
    }

    [Fact]
    public void StripHtml_HandlesComplexHtml()
    {
        var html = "<div class=\"rte\"><h2>Title</h2><p>First paragraph.</p><ul><li>Item 1</li><li>Item 2</li></ul></div>";
        var result = AITextSanitizer.StripHtml(html);

        result.ShouldNotContain("<");
        result.ShouldNotContain(">");
        result.ShouldContain("Title");
        result.ShouldContain("First paragraph.");
        result.ShouldContain("Item 1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripHtml_ReturnsInputForEmptyOrWhitespace(string? input)
    {
        var result = AITextSanitizer.StripHtml(input!);
        result.ShouldBe(input);
    }

    [Fact]
    public void StripHtml_PlainTextPassesThrough()
    {
        var result = AITextSanitizer.StripHtml("Just plain text here.");
        result.ShouldBe("Just plain text here.");
    }
}

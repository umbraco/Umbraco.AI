using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Core.SemanticSearch;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Xunit;

namespace Umbraco.AI.Tests.Unit.SemanticSearch;

public class ContentTextExtractorTests
{
    private readonly ContentTextExtractor _extractor;

    public ContentTextExtractorTests()
    {
        var options = Options.Create(new AISemanticSearchOptions { MaxTextLength = 8000 });
        _extractor = new ContentTextExtractor(options);
    }

    [Fact]
    public void ExtractText_WithNameAndStringProperties_ReturnsExpectedText()
    {
        // Arrange
        var property = CreateStringProperty("Hello World");
        var content = CreateContent("Test Page", [property]);

        // Act
        var result = _extractor.ExtractText(content);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("Test Page");
        result.ShouldContain("Hello World");
    }

    [Fact]
    public void ExtractText_WithHtmlProperty_StripsHtmlTags()
    {
        // Arrange
        var html = new Mock<IHtmlEncodedString>();
        html.Setup(h => h.ToHtmlString()).Returns("<p>Some <strong>bold</strong> text</p>");

        var property = CreateTypedProperty(html.Object);
        var content = CreateContent("HTML Page", [property]);

        // Act
        var result = _extractor.ExtractText(content);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("Some bold text");
        result.ShouldNotContain("<p>");
        result.ShouldNotContain("<strong>");
    }

    [Fact]
    public void ExtractText_WithCollectionProperty_JoinsWithCommas()
    {
        // Arrange
        var tags = new List<string> { "tag1", "tag2", "tag3" };
        var property = CreateTypedProperty(tags);
        var content = CreateContent("Tagged Page", [property]);

        // Act
        var result = _extractor.ExtractText(content);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("tag1, tag2, tag3");
    }

    [Fact]
    public void ExtractText_WithNoMeaningfulText_ReturnsNull()
    {
        // Arrange
        var property = CreateTypedProperty(42); // Non-text value
        var content = CreateContent("", [property]);

        // Act
        var result = _extractor.ExtractText(content);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractText_ExceedsMaxLength_Truncates()
    {
        // Arrange
        var options = Options.Create(new AISemanticSearchOptions { MaxTextLength = 50 });
        var extractor = new ContentTextExtractor(options);

        var longText = new string('A', 100);
        var property = CreateStringProperty(longText);
        var content = CreateContent("Title", [property]);

        // Act
        var result = extractor.ExtractText(content);

        // Assert
        result.ShouldNotBeNull();
        result!.Length.ShouldBe(50);
    }

    [Fact]
    public void ExtractText_NullPropertyValues_SkipsGracefully()
    {
        // Arrange
        var property = CreateTypedProperty(null);
        var content = CreateContent("Page", [property]);

        // Act
        var result = _extractor.ExtractText(content);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("Page");
    }

    private static IPublishedContent CreateContent(string name, IPublishedProperty[] properties)
    {
        var content = new Mock<IPublishedContent>();
        content.Setup(c => c.Name).Returns(name);
        content.Setup(c => c.Properties).Returns(properties);
        return content.Object;
    }

    private static IPublishedProperty CreateStringProperty(string value)
    {
        return CreateTypedProperty(value);
    }

    private static IPublishedProperty CreateTypedProperty(object? value)
    {
        var property = new Mock<IPublishedProperty>();
        property.Setup(p => p.GetValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(value);
        return property.Object;
    }
}

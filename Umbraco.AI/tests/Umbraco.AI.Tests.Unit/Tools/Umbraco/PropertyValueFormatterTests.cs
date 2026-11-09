using Moq;

using Shouldly;

using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class PropertyValueFormatterTests
{
    private const string RichTextEditorAlias = "Umbraco.RichText";

    [Fact]
    public void ExtractProperties_RichTextHtmlEncodedString_ReturnsPlainText()
    {
        var content = BuildContent("bodyText", RichTextEditorAlias, new HtmlEncodedString("<p>Hello <strong>world</strong></p>"));

        var properties = PropertyValueFormatter.ExtractProperties(content);

        var property = properties.ShouldHaveSingleItem();
        property.Alias.ShouldBe("bodyText");
        property.EditorAlias.ShouldBe(RichTextEditorAlias);
        property.Value.ShouldBe("Hello world");
    }

    [Fact]
    public void ExtractProperties_RichTextHtmlEncodedString_DecodesEntitiesAndCollapsesWhitespace()
    {
        var content = BuildContent(
            "bodyText",
            RichTextEditorAlias,
            new HtmlEncodedString("<p>Tom &amp; Jerry</p>\n<p>  were   friends </p>"));

        var properties = PropertyValueFormatter.ExtractProperties(content);

        properties.ShouldHaveSingleItem().Value.ShouldBe("Tom & Jerry were friends");
    }

    [Fact]
    public void ExtractProperties_RichTextEmptyHtmlEncodedString_ReturnsNull()
    {
        var content = BuildContent("bodyText", RichTextEditorAlias, new HtmlEncodedString(string.Empty));

        var properties = PropertyValueFormatter.ExtractProperties(content);

        properties.ShouldHaveSingleItem().Value.ShouldBeNull();
    }

    [Fact]
    public void ExtractProperties_RichTextString_ReturnsPlainText()
    {
        // Regression guard: the existing string handling is unchanged.
        var content = BuildContent("bodyText", RichTextEditorAlias, "<p>Plain <em>string</em></p>");

        var properties = PropertyValueFormatter.ExtractProperties(content);

        properties.ShouldHaveSingleItem().Value.ShouldBe("Plain string");
    }

    private static IPublishedContent BuildContent(string alias, string editorAlias, object? value)
    {
        var dataType = new PublishedDataType(1, editorAlias, editorAlias, new Lazy<object?>(() => null));

        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.Setup(x => x.EditorAlias).Returns(editorAlias);
        propertyType.Setup(x => x.DataType).Returns(dataType);

        var property = new Mock<IPublishedProperty>();
        property.Setup(x => x.Alias).Returns(alias);
        property.Setup(x => x.PropertyType).Returns(propertyType.Object);
        property.Setup(x => x.GetValue(It.IsAny<string?>(), It.IsAny<string?>())).Returns(value);

        var content = new Mock<IPublishedContent>();
        content.Setup(x => x.Properties).Returns([property.Object]);

        return content.Object;
    }
}

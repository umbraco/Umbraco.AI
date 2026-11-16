using System.Text.Json;

using Moq;

using Shouldly;

using Umbraco.AI.Core;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models;
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

    [Fact]
    public void ExtractProperties_MarkdownHtmlEncodedString_ReturnsPlainText()
    {
        var content = BuildContent("intro", "Umbraco.MarkdownEditor", new HtmlEncodedString("<h1>Title</h1>\n<p>Some <em>text</em></p>"));

        var properties = PropertyValueFormatter.ExtractProperties(content);

        properties.ShouldHaveSingleItem().Value.ShouldBe("Title Some text");
    }

    [Fact]
    public void ExtractProperties_UnrecognisedValueHoldingPublishedElements_SerializesAsShortReferences()
    {
        // Mirrors Commerce variants: a converter model whose items hold published elements, and a
        // content type whose property types point back to it. Passed through raw, it failed
        // serialization for the AI provider with an object cycle.
        var elementKey = Guid.NewGuid();
        var contentType = new Mock<IPublishedContentType>();
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.Setup(x => x.Alias).Returns("sku");
        propertyType.Setup(x => x.ContentType).Returns(contentType.Object);
        contentType.Setup(x => x.Alias).Returns("variant");
        contentType.Setup(x => x.PropertyTypes).Returns([propertyType.Object]);
        var element = new Mock<IPublishedElement>();
        element.Setup(x => x.Key).Returns(elementKey);
        element.Setup(x => x.ContentType).Returns(contentType.Object);
        var content = BuildContent("variants", "Some.Package.Editor", new[] { new VariantItem(element.Object, "Red") });

        var properties = PropertyValueFormatter.ExtractProperties(content);

        var json = JsonSerializer.Serialize(properties, Constants.DefaultJsonSerializerOptions);
        json.ShouldContain($"\"content\":{{\"key\":\"{elementKey}\",\"name\":null,\"contentType\":\"variant\"}}", Case.Sensitive);
        json.ShouldContain("\"name\":\"Red\"", Case.Sensitive);
        json.ShouldNotContain("propertyTypes");
    }

    [Fact]
    public void ExtractProperties_UnrecognisedValues_KeepScalarsAndStructureAndFallBackToText()
    {
        var link = new Link { Name = "Home", Url = "/home", Type = LinkType.Content };

        var linkValue = PropertyValueFormatter.ExtractProperties(BuildContent("link", "Umbraco.MultiUrlPicker", link))
            .ShouldHaveSingleItem().Value.ShouldBeOfType<JsonElement>();
        linkValue.GetProperty("url").GetString().ShouldBe("/home");
        linkValue.GetProperty("type").GetString().ShouldBe("Content");

        PropertyValueFormatter.ExtractProperties(BuildContent("count", "Umbraco.Integer", 42))
            .ShouldHaveSingleItem().Value.ShouldBe(42);
        PropertyValueFormatter.ExtractProperties(BuildContent("type", "Some.Editor", LinkType.Media))
            .ShouldHaveSingleItem().Value.ShouldBe("Media");
        PropertyValueFormatter.ExtractProperties(BuildContent("broken", "Some.Editor", new ThrowingValue()))
            .ShouldHaveSingleItem().Value.ShouldBe("throwing value");
    }

    public sealed record VariantItem(IPublishedElement Content, string Name);

    public sealed class ThrowingValue
    {
        public string Value => throw new InvalidCastException("getter failed");

        public override string ToString() => "throwing value";
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

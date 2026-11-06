using System.Text.Json.Nodes;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class BlockListPropertyValueHandlerTests
{
    private const string LayoutKey = "Umbraco.BlockList";

    [Fact]
    public async Task AddItemAsync_AddsLayoutContentExposeEntries_ForInvariantDocument()
    {
        // Arrange
        var elementTypeKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeService(elementTypeKey, propertyAlias: "title", editorAlias: "Umbraco.TextBox");
        var handler = new BlockListPropertyValueHandler(contentTypeService);

        var context = BuildContext(variants: [new AIVariantId(null, null)]);
        var args = new AIAddItemArgs(
            ElementType: elementTypeKey.ToString(),
            Values: new JsonObject { ["title"] = "Hello" });

        // Act
        var result = await handler.AddItemAsync(value: null, args, context);

        // Assert
        var envelope = result.Value as JsonObject;
        envelope.ShouldNotBeNull();

        var layout = (JsonArray)envelope![BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layout.Count.ShouldBe(1);
        layout[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(result.BlockKey);

        var contentData = (JsonArray)envelope[BlockEnvelopeOps.ContentDataPropertyName]!;
        contentData.Count.ShouldBe(1);
        contentData[0]!["contentTypeKey"]!.GetValue<Guid>().ShouldBe(elementTypeKey);

        var values = (JsonArray)contentData[0]!["values"]!;
        values.Count.ShouldBe(1);
        values[0]!["alias"]!.GetValue<string>().ShouldBe("title");
        values[0]!["value"]!.GetValue<string>().ShouldBe("Hello");
        (values[0]!["culture"]?.GetValue<string?>()).ShouldBeNull();

        var expose = (JsonArray)envelope[BlockEnvelopeOps.ExposePropertyName]!;
        expose.Count.ShouldBe(1);
        expose[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(result.BlockKey);
        (expose[0]!["culture"]?.GetValue<string?>()).ShouldBeNull();
    }

    [Fact]
    public async Task AddItemAsync_AddsOneExposeEntryPerActiveVariant_ForCultureVariantElementType()
    {
        // Arrange
        var elementTypeKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeService(
            elementTypeKey, ContentVariation.Culture, ("title", ContentVariation.Culture));
        var handler = new BlockListPropertyValueHandler(contentTypeService);

        var context = BuildContext(variants:
        [
            new AIVariantId("en-US", null),
            new AIVariantId("da-DK", null),
        ]);

        var args = new AIAddItemArgs(elementTypeKey.ToString(), new JsonObject { ["title"] = "Hi" });

        // Act
        var result = await handler.AddItemAsync(value: null, args, context);

        // Assert
        var expose = (JsonArray)((JsonObject)result.Value!)[BlockEnvelopeOps.ExposePropertyName]!;
        expose.Count.ShouldBe(2);
        expose.Select(e => e!["culture"]!.GetValue<string?>()).ShouldBe(["en-US", "da-DK"]);
    }

    [Fact]
    public async Task AddItemAsync_InvariantElementType_OnVariantDocument_WritesInvariantValuesAndExpose()
    {
        // Arrange — regression test for umbraco/Umbraco.AI#450: a culture-variant block list holding an
        // invariant element type must not tag the new block's values/expose with the active culture,
        // or the backoffice renders it empty/unexposed and save drops it on non-default cultures.
        var elementTypeKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeService(
            elementTypeKey, ContentVariation.Nothing, ("text", ContentVariation.Nothing));
        var handler = new BlockListPropertyValueHandler(contentTypeService);

        var context = BuildContext(variants: [new AIVariantId("en-US", null)]);
        var args = new AIAddItemArgs(elementTypeKey.ToString(), new JsonObject { ["text"] = "Hello" });

        // Act
        var result = await handler.AddItemAsync(value: null, args, context);

        // Assert
        var envelope = (JsonObject)result.Value!;
        var values = (JsonArray)((JsonArray)envelope[BlockEnvelopeOps.ContentDataPropertyName]!)[0]!["values"]!;
        (values[0]!["culture"]?.GetValue<string?>()).ShouldBeNull();

        var expose = (JsonArray)envelope[BlockEnvelopeOps.ExposePropertyName]!;
        expose.Count.ShouldBe(1);
        (expose[0]!["culture"]?.GetValue<string?>()).ShouldBeNull();
    }

    [Fact]
    public async Task AddItemAsync_CultureVariantElementType_TagsOnlyCultureVariantProperties()
    {
        // Arrange
        var elementTypeKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeService(
            elementTypeKey,
            ContentVariation.Culture,
            ("title", ContentVariation.Culture),
            ("code", ContentVariation.Nothing));
        var handler = new BlockListPropertyValueHandler(contentTypeService);

        var context = BuildContext(variants: [new AIVariantId("nl-NL", null)]);
        var args = new AIAddItemArgs(
            elementTypeKey.ToString(),
            new JsonObject { ["title"] = "Hallo", ["code"] = "X1" });

        // Act
        var result = await handler.AddItemAsync(value: null, args, context);

        // Assert
        var envelope = (JsonObject)result.Value!;
        var values = (JsonArray)((JsonArray)envelope[BlockEnvelopeOps.ContentDataPropertyName]!)[0]!["values"]!;
        var cultures = values.ToDictionary(
            v => v!["alias"]!.GetValue<string>(),
            v => v!["culture"]?.GetValue<string?>());
        cultures["title"].ShouldBe("nl-NL");
        cultures["code"].ShouldBeNull();

        var expose = (JsonArray)envelope[BlockEnvelopeOps.ExposePropertyName]!;
        expose.Count.ShouldBe(1);
        expose[0]!["culture"]!.GetValue<string>().ShouldBe("nl-NL");
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesAllReferencesToBlockKey()
    {
        // Arrange
        var contentTypeService = new Mock<IContentTypeService>().Object;
        var handler = new BlockListPropertyValueHandler(contentTypeService);

        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();

        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["contentKey"] = keep });
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["contentKey"] = remove });
        BlockEnvelopeOps.AddContentDataEntry(envelope, Guid.NewGuid(), null, keep);
        BlockEnvelopeOps.AddContentDataEntry(envelope, Guid.NewGuid(), null, remove);

        var context = BuildContext();

        // Act
        var result = await handler.RemoveItemAsync(envelope, remove, context);

        // Assert
        var resultEnvelope = (JsonObject)result!;
        var layout = (JsonArray)resultEnvelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layout.Count.ShouldBe(1);
        layout[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(keep);
    }

    [Fact]
    public async Task AddItemAsync_NewLayoutItem_CarriesLayoutKey()
    {
        var elementTypeKey = Guid.NewGuid();
        var handler = new BlockListPropertyValueHandler(BuildContentTypeService(elementTypeKey, "title", "Umbraco.TextBox"));

        var result = await handler.AddItemAsync(null, new AIAddItemArgs(elementTypeKey.ToString(), new JsonObject { ["title"] = "x" }), BuildContext());

        var layout = (JsonArray)((JsonObject)result.Value!)[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layout[0]!["key"]!.GetValue<Guid>().ShouldBe(result.BlockKey);
    }

    [Fact]
    public async Task AddItemAsync_SegmentedVariants_ExposeOncePerCulture()
    {
        var elementTypeKey = Guid.NewGuid();
        var handler = new BlockListPropertyValueHandler(BuildContentTypeService(
            elementTypeKey, ContentVariation.CultureAndSegment, ("title", ContentVariation.CultureAndSegment)));
        var context = BuildContext(variants: [new AIVariantId("en-US", null), new AIVariantId("en-US", "vip")]);

        var result = await handler.AddItemAsync(null, new AIAddItemArgs(elementTypeKey.ToString(), new JsonObject { ["title"] = "x" }), context);

        var expose = (JsonArray)((JsonObject)result.Value!)[BlockEnvelopeOps.ExposePropertyName]!;
        expose.Count.ShouldBe(1);
        expose[0]!["culture"]!.GetValue<string>().ShouldBe("en-US");
        ((JsonObject)expose[0]!).ContainsKey("segment").ShouldBeFalse();
    }

    [Fact]
    public async Task AddItemAsync_CultureAndSegmentProperty_WritesValueWithCultureAndSegment()
    {
        var elementTypeKey = Guid.NewGuid();
        var handler = new BlockListPropertyValueHandler(BuildContentTypeService(
            elementTypeKey, ContentVariation.CultureAndSegment, ("title", ContentVariation.CultureAndSegment)));
        var context = BuildContext(variants: [new AIVariantId("en-US", "vip")]);

        var result = await handler.AddItemAsync(null, new AIAddItemArgs(elementTypeKey.ToString(), new JsonObject { ["title"] = "x" }), context);

        var values = (JsonArray)((JsonArray)((JsonObject)result.Value!)[BlockEnvelopeOps.ContentDataPropertyName]!)[0]!["values"]!;
        values[0]!["culture"]!.GetValue<string>().ShouldBe("en-US");
        values[0]!["segment"]!.GetValue<string>().ShouldBe("vip");
    }

    [Fact]
    public void ValidateRemoveItem_RepeatedContentKey_IsRejected()
    {
        var library = Guid.NewGuid();
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["key"] = Guid.NewGuid(), ["contentKey"] = library, ["isExternalContent"] = true });
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["key"] = Guid.NewGuid(), ["contentKey"] = library, ["isExternalContent"] = true });
        var handler = new BlockListPropertyValueHandler(new Mock<IContentTypeService>().Object);

        var result = handler.ValidateRemoveItem(envelope, library, BuildContext());

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void GetItemContentTypeKey_ExternalBlock_ReturnsNull()
    {
        var library = Guid.NewGuid();
        var layoutKey = Guid.NewGuid();
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["key"] = layoutKey, ["contentKey"] = library, ["isExternalContent"] = true });
        var handler = new BlockListPropertyValueHandler(new Mock<IContentTypeService>().Object);

        handler.GetItemContentTypeKey(envelope, layoutKey, BuildContext()).ShouldBeNull();
    }

    [Fact]
    public async Task SetItemPropertyValueAsync_ExternalBlock_LeavesEnvelopeUntouched()
    {
        var library = Guid.NewGuid();
        var layoutKey = Guid.NewGuid();
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["key"] = layoutKey, ["contentKey"] = library, ["isExternalContent"] = true });
        var handler = new BlockListPropertyValueHandler(new Mock<IContentTypeService>().Object);

        var result = await handler.SetItemPropertyValueAsync(envelope, layoutKey, "title", JsonValue.Create("x"), null, BuildContext());

        ((JsonArray)((JsonObject)result!)[BlockEnvelopeOps.ContentDataPropertyName]!).Count.ShouldBe(0);
    }

    [Fact]
    public async Task ClearAsync_ReturnsCanonicalEmptyEnvelope()
    {
        var handler = new BlockListPropertyValueHandler(new Mock<IContentTypeService>().Object);

        var existing = new JsonObject
        {
            [BlockEnvelopeOps.LayoutPropertyName] = new JsonObject
            {
                [LayoutKey] = new JsonArray { new JsonObject { ["contentKey"] = Guid.NewGuid() } },
            },
        };

        var result = await handler.ClearAsync(existing, BuildContext());

        var envelope = (JsonObject)result!;
        ((JsonArray)envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!).Count.ShouldBe(0);
        envelope[BlockEnvelopeOps.ContentDataPropertyName].ShouldBeOfType<JsonArray>();
    }

    [Fact]
    public void GetItemContentTypeKey_ReturnsBlockContentType()
    {
        var contentTypeKey = Guid.NewGuid();
        var blockKey = Guid.NewGuid();

        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        BlockEnvelopeOps.AddContentDataEntry(envelope, contentTypeKey, null, blockKey);

        var handler = new BlockListPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var result = handler.GetItemContentTypeKey(envelope, blockKey, BuildContext());

        result.ShouldBe(contentTypeKey);
    }

    private static IContentTypeService BuildContentTypeService(Guid contentTypeKey, string propertyAlias, string editorAlias)
        => BuildContentTypeService(contentTypeKey, ContentVariation.Nothing, [(propertyAlias, editorAlias, ContentVariation.Nothing)]);

    private static IContentTypeService BuildContentTypeService(
        Guid contentTypeKey,
        ContentVariation elementVariations,
        params (string Alias, ContentVariation Variations)[] properties)
        => BuildContentTypeService(
            contentTypeKey,
            elementVariations,
            properties.Select(p => (p.Alias, "Umbraco.TextBox", p.Variations)).ToArray());

    private static IContentTypeService BuildContentTypeService(
        Guid contentTypeKey,
        ContentVariation elementVariations,
        (string Alias, string EditorAlias, ContentVariation Variations)[] properties)
    {
        var propertyTypes = properties.Select(p =>
        {
            var propertyType = new Mock<IPropertyType>();
            propertyType.Setup(x => x.Alias).Returns(p.Alias);
            propertyType.Setup(x => x.PropertyEditorAlias).Returns(p.EditorAlias);
            propertyType.Setup(x => x.Variations).Returns(p.Variations);
            return propertyType.Object;
        }).ToArray();

        var contentType = new Mock<IContentType>();
        contentType.Setup(c => c.Key).Returns(contentTypeKey);
        contentType.Setup(c => c.Variations).Returns(elementVariations);
        contentType.As<IContentTypeComposition>()
            .Setup(c => c.CompositionPropertyTypes)
            .Returns(propertyTypes);

        var service = new Mock<IContentTypeService>();
        service.Setup(s => s.Get(contentTypeKey)).Returns(contentType.Object);
        return service.Object;
    }

    private static AIPropertyValueOperationContext BuildContext(IReadOnlyList<AIVariantId>? variants = null)
    {
        var schemaService = new Mock<IPropertyEditorSchemaService>();
        schemaService.Setup(s => s.GetSchemaAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Attempt<PropertyValueSchema, PropertyEditorSchemaOperationStatus>.Fail(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        var defaultValueProvider = new Mock<IAIPropertyDefaultValueProvider>();
        var dispatcher = new Mock<IAIPropertyValueDispatcher>();

        return new AIPropertyValueOperationContext(
            schemaService.Object,
            defaultValueProvider.Object,
            new AIDocumentMetadata(
                ContentTypeKey: Guid.NewGuid(),
                Variants: variants ?? [new AIVariantId(null, null)],
                IsVariant: variants is { Count: > 1 } || (variants?[0]?.Culture is not null),
                IsSegmented: false),
            dispatcher.Object);
    }
}

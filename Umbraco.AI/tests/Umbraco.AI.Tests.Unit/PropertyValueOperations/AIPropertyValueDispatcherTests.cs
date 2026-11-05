using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.PropertyValueOperations.Handlers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class AIPropertyValueDispatcherTests
{
    private const string TestEditor = "Test.Editor";
    private const string OuterEditor = "Test.Outer";
    private const string InnerEditor = "Test.Inner";

    private static readonly Guid RootContentTypeKey = Guid.NewGuid();

    private static readonly AIDocumentMetadata Metadata = new(
        ContentTypeKey: RootContentTypeKey,
        Variants: [new AIVariantId(null, null)],
        IsVariant: false,
        IsSegmented: false,
        Name: "Test");

    [Fact]
    public async Task DispatchAsync_AddItem_AtRoot_ReturnsNewRootValueWithBlockKey()
    {
        // Arrange
        var handler = new FakePropertyValueHandler(TestEditor);
        var dispatcher = BuildDispatcher(
            handlers: [handler],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("contentBlocks")],
            Operation: AIPropertyOperation.AddItem,
            Args: new JsonObject
            {
                ["values"] = new JsonObject { ["title"] = "Hello" },
            },
            RootValue: new JsonObject { ["items"] = new JsonArray() },
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.BlockKey.ShouldNotBeNull();

        var items = result.NewRootValue?["items"] as JsonArray;
        items.ShouldNotBeNull();
        items!.Count.ShouldBe(1);
        items[0]!["values"]!["title"]!.GetValue<string>().ShouldBe("Hello");
    }

    [Fact]
    public async Task DispatchAsync_AddItem_NestedDepth2_DescendsAndAscendsCorrectly()
    {
        // Arrange
        var innerContentTypeKey = Guid.NewGuid();
        var existingBlockKey = Guid.NewGuid();

        var outerHandler = new FakePropertyValueHandler(OuterEditor);
        var innerHandler = new FakePropertyValueHandler(InnerEditor);

        // Two content types: the document's root maps "rows" → OuterEditor, and the block's
        // element type maps "innerBlocks" → InnerEditor (the second resolution happens during
        // descent via the block handler's GetItemContentTypeKey).
        var contentTypeService = BuildContentTypeServiceWithTypes(
            (RootContentTypeKey, new Dictionary<string, string> { ["rows"] = OuterEditor }),
            (innerContentTypeKey, new Dictionary<string, string> { ["innerBlocks"] = InnerEditor }));

        var dispatcher = BuildDispatcher(
            handlers: [outerHandler, innerHandler],
            contentTypeService: contentTypeService);

        // Existing outer envelope: one block whose innerBlocks property holds an empty inner envelope.
        var rootValue = new JsonObject
        {
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["blockKey"] = existingBlockKey,
                    ["contentTypeKey"] = innerContentTypeKey,
                    ["values"] = new JsonObject
                    {
                        ["innerBlocks"] = new JsonObject { ["items"] = new JsonArray() },
                    },
                },
            },
        };

        var request = new AIPropertyValueDispatchRequest(
            Path:
            [
                AIPropertyPathSegment.ForProperty("rows"),
                AIPropertyPathSegment.ForBlock(existingBlockKey),
                AIPropertyPathSegment.ForProperty("innerBlocks"),
            ],
            Operation: AIPropertyOperation.AddItem,
            Args: new JsonObject
            {
                ["values"] = new JsonObject { ["title"] = "Nested" },
            },
            RootValue: rootValue,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        result.BlockKey.ShouldNotBeNull();

        // The outer envelope must still have one item, whose innerBlocks now contains the new inner block.
        var outerItems = result.NewRootValue?["items"] as JsonArray;
        outerItems.ShouldNotBeNull();
        outerItems!.Count.ShouldBe(1);

        var innerItems = outerItems[0]!["values"]!["innerBlocks"]!["items"] as JsonArray;
        innerItems.ShouldNotBeNull();
        innerItems!.Count.ShouldBe(1);
        innerItems[0]!["values"]!["title"]!.GetValue<string>().ShouldBe("Nested");
    }

    [Fact]
    public void TryReadValue_NestedPath_ReturnsValueInsideBlock()
    {
        // Arrange
        var innerContentTypeKey = Guid.NewGuid();
        var blockKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeServiceWithTypes(
            (RootContentTypeKey, new Dictionary<string, string> { ["rows"] = OuterEditor }),
            (innerContentTypeKey, new Dictionary<string, string> { ["heading"] = InnerEditor }));
        IAIPropertyValueReader reader = BuildDispatcher(
            handlers: [new FakePropertyValueHandler(OuterEditor)],
            contentTypeService: contentTypeService);

        var request = new AIPropertyValueDispatchRequest(
            Path:
            [
                AIPropertyPathSegment.ForProperty("rows"),
                AIPropertyPathSegment.ForBlock(blockKey),
                AIPropertyPathSegment.ForProperty("heading"),
            ],
            Operation: AIPropertyOperation.SetValue,
            Args: null,
            RootValue: new JsonObject
            {
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["blockKey"] = blockKey,
                        ["contentTypeKey"] = innerContentTypeKey,
                        ["values"] = new JsonObject { ["heading"] = "Autumn" },
                    },
                },
            },
            DocumentMetadata: Metadata);

        // Act
        var found = reader.TryReadValue(request, out var value);

        // Assert
        found.ShouldBeTrue();
        value!.GetValue<string>().ShouldBe("Autumn");
    }

    [Fact]
    public void TryReadValue_BlockMissing_ReturnsFalse()
    {
        // Arrange
        IAIPropertyValueReader reader = BuildDispatcher(
            handlers: [new FakePropertyValueHandler(TestEditor)],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });

        var request = new AIPropertyValueDispatchRequest(
            Path:
            [
                AIPropertyPathSegment.ForProperty("contentBlocks"),
                AIPropertyPathSegment.ForBlock(Guid.NewGuid()),
                AIPropertyPathSegment.ForProperty("heading"),
            ],
            Operation: AIPropertyOperation.SetValue,
            Args: null,
            RootValue: new JsonObject { ["items"] = new JsonArray() },
            DocumentMetadata: Metadata);

        // Act
        var found = reader.TryReadValue(request, out var value);

        // Assert
        found.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public async Task DispatchAsync_RemoveItem_AtRoot_RemovesByBlockKey()
    {
        // Arrange
        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();
        var handler = new FakePropertyValueHandler(TestEditor);
        var dispatcher = BuildDispatcher(
            handlers: [handler],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });

        var rootValue = new JsonObject
        {
            ["items"] = new JsonArray
            {
                new JsonObject { ["blockKey"] = keep, ["values"] = new JsonObject() },
                new JsonObject { ["blockKey"] = remove, ["values"] = new JsonObject() },
            },
        };

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("contentBlocks")],
            Operation: AIPropertyOperation.RemoveItem,
            Args: new JsonObject { ["blockKey"] = remove.ToString() },
            RootValue: rootValue,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        var items = result.NewRootValue?["items"] as JsonArray;
        items.ShouldNotBeNull();
        items!.Count.ShouldBe(1);
        items[0]!["blockKey"]!.GetValue<Guid>().ShouldBe(keep);
    }

    [Fact]
    public async Task DispatchAsync_RemoveItem_BlockGrid_NestedInArea_RejectsWithoutMutatingContentData()
    {
        // Arrange — regression test for umbraco/Umbraco.AI#397: deleting a block nested inside
        // another block's area must not silently delete its contentData while leaving the layout
        // entry (and settings) behind.
        const string layoutKey = "Umbraco.BlockGrid";
        var rootContentKey = Guid.NewGuid();
        var nestedContentKey = Guid.NewGuid();

        var handler = new BlockGridPropertyValueHandler(new Mock<IContentTypeService>().Object);
        var dispatcher = BuildDispatcher(
            handlers: [handler],
            rootProperties: new Dictionary<string, string> { ["content"] = "Umbraco.BlockGrid" });

        var rootValue = new JsonObject
        {
            ["layout"] = new JsonObject
            {
                [layoutKey] = new JsonArray
                {
                    new JsonObject
                    {
                        ["contentKey"] = rootContentKey,
                        ["columnSpan"] = 12,
                        ["rowSpan"] = 1,
                        ["areas"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["key"] = Guid.NewGuid(),
                                ["items"] = new JsonArray
                                {
                                    new JsonObject
                                    {
                                        ["contentKey"] = nestedContentKey,
                                        ["columnSpan"] = 12,
                                        ["rowSpan"] = 1,
                                        ["areas"] = new JsonArray(),
                                    },
                                },
                            },
                        },
                    },
                },
            },
            ["contentData"] = new JsonArray
            {
                new JsonObject { ["key"] = rootContentKey, ["contentTypeKey"] = Guid.NewGuid(), ["values"] = new JsonArray() },
                new JsonObject { ["key"] = nestedContentKey, ["contentTypeKey"] = Guid.NewGuid(), ["values"] = new JsonArray() },
            },
            ["settingsData"] = new JsonArray(),
            ["expose"] = new JsonArray(),
        };

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("content")],
            Operation: AIPropertyOperation.RemoveItem,
            Args: new JsonObject { ["blockKey"] = nestedContentKey.ToString() },
            RootValue: rootValue,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.OperationNotSupported);

        // The root value handed back on failure is the dispatcher's default (unset); the caller
        // must not persist a mutated value. Confirm the source root value itself was untouched.
        var contentData = rootValue["contentData"] as JsonArray;
        contentData!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task DispatchAsync_RemoveItem_WithNonStringBlockKey_ReturnsInvalidPathInsteadOfThrowing()
    {
        // Arrange
        var handler = new FakePropertyValueHandler(TestEditor);
        var dispatcher = BuildDispatcher(
            handlers: [handler],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });

        var rootValue = new JsonObject
        {
            ["items"] = new JsonArray
            {
                new JsonObject { ["blockKey"] = Guid.NewGuid(), ["values"] = new JsonObject() },
            },
        };

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("contentBlocks")],
            Operation: AIPropertyOperation.RemoveItem,
            Args: new JsonObject { ["blockKey"] = 123 },
            RootValue: rootValue,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.InvalidPath);
        result.Error.Message.ShouldContain("blockKey");
    }

    [Fact]
    public async Task DispatchAsync_SetValue_AtRoot_ReplacesValue()
    {
        // Arrange
        var dispatcher = BuildDispatcher(
            handlers: [],
            rootProperties: new Dictionary<string, string> { ["title"] = "Umbraco.TextBox" });

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("title")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "Replaced" },
            RootValue: JsonValue.Create("Original"),
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        result.NewRootValue?.GetValue<string>().ShouldBe("Replaced");
    }

    [Fact]
    public async Task DispatchAsync_ClearValue_WithHandler_DefersToHandlerEmptyRepresentation()
    {
        // Arrange
        var handler = new FakePropertyValueHandler(TestEditor);
        var dispatcher = BuildDispatcher(
            handlers: [handler],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });

        var rootValue = new JsonObject
        {
            ["items"] = new JsonArray
            {
                new JsonObject { ["blockKey"] = Guid.NewGuid(), ["values"] = new JsonObject() },
            },
        };

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("contentBlocks")],
            Operation: AIPropertyOperation.ClearValue,
            Args: null,
            RootValue: rootValue,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        var items = result.NewRootValue?["items"] as JsonArray;
        items.ShouldNotBeNull();
        items!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DispatchAsync_AddItem_NoHandler_ReturnsNoHandlerError()
    {
        // Arrange
        // The property resolves to "Unknown.Editor" but no handler is registered for that alias.
        var dispatcher = BuildDispatcher(
            handlers: [],
            rootProperties: new Dictionary<string, string> { ["p"] = "Unknown.Editor" });

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("p")],
            Operation: AIPropertyOperation.AddItem,
            Args: null,
            RootValue: null,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNull();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.NoHandler);
    }

    [Fact]
    public async Task DispatchAsync_RootPropertyNotOnContentType_ReturnsPropertyNotFoundError()
    {
        // Arrange
        // The path references a property that doesn't exist on the document's content type, so
        // the dispatcher's editor-alias resolution fails before any handler is consulted.
        var dispatcher = BuildDispatcher(
            handlers: [],
            rootProperties: new Dictionary<string, string>());

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("doesNotExist")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "x" },
            RootValue: null,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.PropertyNotFound);
    }

    [Fact]
    public async Task DispatchAsync_EmptyPath_ReturnsInvalidPathError()
    {
        // Arrange
        var dispatcher = BuildDispatcher(handlers: []);

        var request = new AIPropertyValueDispatchRequest(
            Path: Array.Empty<AIPropertyPathSegment>(),
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "x" },
            RootValue: null,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.InvalidPath);
    }

    [Fact]
    public async Task DispatchAsync_PathStartingWithBlockKey_ReturnsInvalidPathError()
    {
        // Arrange
        var dispatcher = BuildDispatcher(handlers: []);

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForBlock(Guid.NewGuid())],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "x" },
            RootValue: null,
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error!.Code.ShouldBe(AIPropertyValueOperationError.Codes.InvalidPath);
    }

    [Fact]
    public async Task DispatchAsync_AddItem_ValidationFails_PropagatesValidationError()
    {
        // Arrange
        var validationError = new AIPropertyValueOperationError(
            AIPropertyValueOperationError.Codes.SchemaMismatch,
            "missing required property: foo");
        var handler = new FakePropertyValueHandler(TestEditor, AIValidationResult.Invalid(validationError));
        var dispatcher = BuildDispatcher(
            handlers: [handler],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("contentBlocks")],
            Operation: AIPropertyOperation.AddItem,
            Args: null,
            RootValue: new JsonObject { ["items"] = new JsonArray() },
            DocumentMetadata: Metadata);

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeFalse();
        result.Error.ShouldBe(validationError);
    }

    [Fact]
    public async Task DispatchAsync_SetValue_InBlock_CultureVariantProperty_UpdatesEditedCultureEntry()
    {
        // Arrange — regression test for umbraco/Umbraco.AI#450: descending into a block used to pass no
        // variant, so the write appended a new culture:null entry and left the edited culture unchanged.
        var (dispatcher, elementTypeKey) = BuildBlockListDispatcher(textVariations: ContentVariation.Culture);
        var blockKey = Guid.NewGuid();
        var rootValue = BuildBlockListValue(
            blockKey,
            elementTypeKey,
            ("en-US", "Hello"),
            ("nl-NL", "Hallo"));

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("blocks"), AIPropertyPathSegment.ForBlock(blockKey), AIPropertyPathSegment.ForProperty("text")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "Goedendag" },
            RootValue: rootValue,
            DocumentMetadata: VariantMetadata("nl-NL"));

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        var values = ReadBlockTextValues(result.NewRootValue);
        values.Count.ShouldBe(2);
        values["en-US"].ShouldBe("Hello");
        values["nl-NL"].ShouldBe("Goedendag");
    }

    [Theory]
    [InlineData(ContentVariation.Culture, true)]
    [InlineData(ContentVariation.Nothing, false)]
    public async Task DispatchAsync_SetValue_InBlock_ReportsWhetherNestedPropertyVariesByCulture(ContentVariation textVariations, bool expected)
    {
        var (dispatcher, elementTypeKey) = BuildBlockListDispatcher(textVariations);
        var blockKey = Guid.NewGuid();
        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("blocks"), AIPropertyPathSegment.ForBlock(blockKey), AIPropertyPathSegment.ForProperty("text")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "x" },
            RootValue: BuildBlockListValue(blockKey, elementTypeKey, ("nl-NL", "Hallo")),
            DocumentMetadata: VariantMetadata("nl-NL"));

        var result = await dispatcher.DispatchAsync(request);

        result.Success.ShouldBeTrue();
        result.NestedVariesByCulture.ShouldBe(expected);
    }

    [Fact]
    public async Task DispatchAsync_SetValue_AtRoot_LeavesNestedVariesByCultureUnset()
    {
        var dispatcher = BuildDispatcher(
            handlers: [new FakePropertyValueHandler(TestEditor)],
            rootProperties: new Dictionary<string, string> { ["contentBlocks"] = TestEditor });
        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("contentBlocks")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "x" },
            RootValue: null,
            DocumentMetadata: Metadata);

        var result = await dispatcher.DispatchAsync(request);

        result.Success.ShouldBeTrue();
        result.NestedVariesByCulture.ShouldBeNull();
    }

    [Fact]
    public async Task DispatchAsync_SetValue_InBlock_ExplicitVariant_TakesPrecedenceOverFirstDocumentVariant()
    {
        // Arrange — split view lists two active variants; the request names the one being edited.
        var (dispatcher, elementTypeKey) = BuildBlockListDispatcher(textVariations: ContentVariation.Culture);
        var blockKey = Guid.NewGuid();
        var rootValue = BuildBlockListValue(blockKey, elementTypeKey, ("en-US", "Hello"), ("nl-NL", "Hallo"));

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("blocks"), AIPropertyPathSegment.ForBlock(blockKey), AIPropertyPathSegment.ForProperty("text")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "Goedendag" },
            RootValue: rootValue,
            DocumentMetadata: VariantMetadata("en-US", "nl-NL"))
        {
            Variant = new AIVariantId("nl-NL", null),
        };

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        var values = ReadBlockTextValues(result.NewRootValue);
        values["en-US"].ShouldBe("Hello");
        values["nl-NL"].ShouldBe("Goedendag");
    }

    [Fact]
    public async Task DispatchAsync_SetValue_InBlock_InvariantProperty_OnVariantDocument_UpdatesInvariantEntry()
    {
        // Arrange
        var (dispatcher, elementTypeKey) = BuildBlockListDispatcher(textVariations: ContentVariation.Nothing);
        var blockKey = Guid.NewGuid();
        var rootValue = BuildBlockListValue(blockKey, elementTypeKey, (null, "Shared"));

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("blocks"), AIPropertyPathSegment.ForBlock(blockKey), AIPropertyPathSegment.ForProperty("text")],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "Updated" },
            RootValue: rootValue,
            DocumentMetadata: VariantMetadata("nl-NL"));

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        var values = ReadBlockTextValues(result.NewRootValue);
        values.Count.ShouldBe(1);
        values[string.Empty].ShouldBe("Updated");
    }

    [Fact]
    public async Task DispatchAsync_AddItem_InNestedCultureVariantBlockList_ReadsAndWritesEditedCulture()
    {
        // Arrange — the nested block list varies by culture, so descending must read the nl-NL entry
        // (not whichever entry is stored first) and write the result back to that same entry.
        var outerElementKey = Guid.NewGuid();
        var innerElementKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeServiceWithPropertyTypes(
            (RootContentTypeKey, [("blocks", "Umbraco.BlockList", ContentVariation.Nothing)]),
            (outerElementKey, [("items", "Umbraco.BlockList", ContentVariation.Culture)]),
            (innerElementKey, [("text", "Umbraco.TextBox", ContentVariation.Nothing)]));
        var dispatcher = BuildDispatcher(
            handlers: [new BlockListPropertyValueHandler(contentTypeService)],
            contentTypeService: contentTypeService);

        var outerBlockKey = Guid.NewGuid();
        var existingInnerKey = Guid.NewGuid();
        var enItems = BuildBlockListValue(existingInnerKey, innerElementKey, (null, "English block"));
        var nlItems = BlockEnvelopeOps.Empty("Umbraco.BlockList");

        var rootValue = BuildBlockListValue(outerBlockKey, outerElementKey);
        var outerValues = (JsonArray)rootValue["contentData"]![0]!["values"]!;
        outerValues.Add(new JsonObject { ["alias"] = "items", ["culture"] = "en-US", ["segment"] = null, ["value"] = enItems });
        outerValues.Add(new JsonObject { ["alias"] = "items", ["culture"] = "nl-NL", ["segment"] = null, ["value"] = nlItems });

        var request = new AIPropertyValueDispatchRequest(
            Path: [AIPropertyPathSegment.ForProperty("blocks"), AIPropertyPathSegment.ForBlock(outerBlockKey), AIPropertyPathSegment.ForProperty("items")],
            Operation: AIPropertyOperation.AddItem,
            Args: new JsonObject { ["elementType"] = innerElementKey.ToString(), ["values"] = new JsonObject { ["text"] = "Nederlands blok" } },
            RootValue: rootValue,
            DocumentMetadata: VariantMetadata("nl-NL"));

        // Act
        var result = await dispatcher.DispatchAsync(request);

        // Assert
        result.Success.ShouldBeTrue();
        var items = ((JsonArray)result.NewRootValue!["contentData"]![0]!["values"]!)
            .ToDictionary(v => v!["culture"]?.GetValue<string?>() ?? string.Empty, v => v!["value"]!);
        items.Count.ShouldBe(2);

        var enContent = (JsonArray)items["en-US"]["contentData"]!;
        enContent.Count.ShouldBe(1);
        enContent[0]!["key"]!.GetValue<Guid>().ShouldBe(existingInnerKey);

        var nlContent = (JsonArray)items["nl-NL"]["contentData"]!;
        nlContent.Count.ShouldBe(1);
        nlContent[0]!["key"]!.GetValue<Guid>().ShouldBe(result.BlockKey!.Value);
    }

    [Theory]
    [InlineData(ContentVariation.Nothing, ContentVariation.Culture, false)]
    [InlineData(ContentVariation.Culture, ContentVariation.Nothing, true)]
    public async Task DispatchAsync_SetValue_TwoBlockLevels_NestedVariesByCultureComesFromFirstNestedProperty(
        ContentVariation firstNestedVariations, ContentVariation leafVariations, bool expected)
    {
        // The flag must reflect the first property below the root, not the leaf and not a later level.
        var outerElementKey = Guid.NewGuid();
        var innerElementKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeServiceWithPropertyTypes(
            (RootContentTypeKey, [("blocks", "Umbraco.BlockList", ContentVariation.Nothing)]),
            (outerElementKey, [("items", "Umbraco.BlockList", firstNestedVariations)]),
            (innerElementKey, [("text", "Umbraco.TextBox", leafVariations)]));
        var dispatcher = BuildDispatcher(
            handlers: [new BlockListPropertyValueHandler(contentTypeService)],
            contentTypeService: contentTypeService);

        var outerBlockKey = Guid.NewGuid();
        var innerBlockKey = Guid.NewGuid();
        var leafCulture = leafVariations.HasFlag(ContentVariation.Culture) ? "nl-NL" : null;
        var innerItems = BuildBlockListValue(innerBlockKey, innerElementKey, (leafCulture, "Hallo"));
        var rootValue = BuildBlockListValue(outerBlockKey, outerElementKey);
        ((JsonArray)rootValue["contentData"]![0]!["values"]!).Add(new JsonObject
        {
            ["alias"] = "items",
            ["culture"] = firstNestedVariations.HasFlag(ContentVariation.Culture) ? "nl-NL" : null,
            ["segment"] = null,
            ["value"] = innerItems,
        });

        var request = new AIPropertyValueDispatchRequest(
            Path:
            [
                AIPropertyPathSegment.ForProperty("blocks"),
                AIPropertyPathSegment.ForBlock(outerBlockKey),
                AIPropertyPathSegment.ForProperty("items"),
                AIPropertyPathSegment.ForBlock(innerBlockKey),
                AIPropertyPathSegment.ForProperty("text"),
            ],
            Operation: AIPropertyOperation.SetValue,
            Args: new JsonObject { ["value"] = "Goedendag" },
            RootValue: rootValue,
            DocumentMetadata: VariantMetadata("nl-NL"));

        var result = await dispatcher.DispatchAsync(request);

        result.Success.ShouldBeTrue();
        result.NestedVariesByCulture.ShouldBe(expected);
    }

    private static AIDocumentMetadata VariantMetadata(params string[] cultures) => new(
        ContentTypeKey: RootContentTypeKey,
        Variants: cultures.Select(c => new AIVariantId(c, null)).ToArray(),
        IsVariant: true,
        IsSegmented: false);

    private static (AIPropertyValueDispatcher Dispatcher, Guid ElementTypeKey) BuildBlockListDispatcher(ContentVariation textVariations)
    {
        var elementTypeKey = Guid.NewGuid();
        var contentTypeService = BuildContentTypeServiceWithPropertyTypes(
            (RootContentTypeKey, [("blocks", "Umbraco.BlockList", ContentVariation.Nothing)]),
            (elementTypeKey, [("text", "Umbraco.TextBox", textVariations)]));

        var dispatcher = BuildDispatcher(
            handlers: [new BlockListPropertyValueHandler(contentTypeService)],
            contentTypeService: contentTypeService);

        return (dispatcher, elementTypeKey);
    }

    private static JsonObject BuildBlockListValue(Guid blockKey, Guid elementTypeKey, params (string? Culture, string Value)[] textValues)
    {
        var values = new JsonArray();
        foreach (var (culture, value) in textValues)
        {
            values.Add(new JsonObject { ["alias"] = "text", ["culture"] = culture, ["segment"] = null, ["value"] = value });
        }

        return new JsonObject
        {
            ["layout"] = new JsonObject { ["Umbraco.BlockList"] = new JsonArray { new JsonObject { ["contentKey"] = blockKey } } },
            ["contentData"] = new JsonArray
            {
                new JsonObject { ["key"] = blockKey, ["contentTypeKey"] = elementTypeKey, ["values"] = values },
            },
            ["settingsData"] = new JsonArray(),
            ["expose"] = new JsonArray(),
        };
    }

    /// <summary>Returns the block's <c>text</c> values keyed by culture (empty string for invariant).</summary>
    private static Dictionary<string, string> ReadBlockTextValues(JsonNode? rootValue)
        => ((JsonArray)rootValue!["contentData"]![0]!["values"]!)
            .Where(v => v!["alias"]!.GetValue<string>() == "text")
            .ToDictionary(
                v => v!["culture"]?.GetValue<string?>() ?? string.Empty,
                v => v!["value"]!.GetValue<string>());

    private static AIPropertyValueDispatcher BuildDispatcher(
        IEnumerable<IAIPropertyValueHandler> handlers,
        IReadOnlyDictionary<string, string>? rootProperties = null,
        IContentTypeService? contentTypeService = null)
    {
        var collection = new AIPropertyValueHandlerCollection(() => handlers);

        var schemaService = new Mock<IPropertyEditorSchemaService>();
        schemaService.Setup(s => s.GetSchemaAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Attempt<PropertyValueSchema, PropertyEditorSchemaOperationStatus>.Fail(
                PropertyEditorSchemaOperationStatus.SchemaNotSupported,
                new PropertyValueSchema(null, null)));

        contentTypeService ??= rootProperties is null
            ? new Mock<IContentTypeService>().Object
            : BuildContentTypeServiceWithTypes((RootContentTypeKey, rootProperties));

        var mediaTypeService = new Mock<IMediaTypeService>().Object;

        var defaultValueProvider = new Mock<IAIPropertyDefaultValueProvider>();
        defaultValueProvider.Setup(p => p.GetDefaultValueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonNode?)null);
        defaultValueProvider.Setup(p => p.GetDefaultValuesForContentTypeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, JsonNode?>());

        return new AIPropertyValueDispatcher(
            collection,
            schemaService.Object,
            contentTypeService,
            mediaTypeService,
            defaultValueProvider.Object,
            NullLogger<AIPropertyValueDispatcher>.Instance);
    }

    /// <summary>
    /// Builds a stub <see cref="IContentTypeService"/> that returns content types whose
    /// <c>CompositionPropertyTypes</c> match the supplied (alias → editor alias) mappings.
    /// Used to drive both root editor-alias resolution and nested property resolution during the
    /// dispatcher's descent walk.
    /// </summary>
    private static IContentTypeService BuildContentTypeServiceWithTypes(
        params (Guid ContentTypeKey, IReadOnlyDictionary<string, string> Properties)[] types)
        => BuildContentTypeServiceWithPropertyTypes(types
            .Select(t => (t.ContentTypeKey, t.Properties.Select(p => (p.Key, p.Value, ContentVariation.Nothing)).ToArray()))
            .ToArray());

    private static IContentTypeService BuildContentTypeServiceWithPropertyTypes(
        params (Guid ContentTypeKey, (string Alias, string EditorAlias, ContentVariation Variations)[] Properties)[] types)
    {
        var service = new Mock<IContentTypeService>();

        foreach (var (key, properties) in types)
        {
            var propertyTypes = properties.Select(p =>
            {
                var pt = new Mock<IPropertyType>();
                pt.Setup(x => x.Alias).Returns(p.Alias);
                pt.Setup(x => x.PropertyEditorAlias).Returns(p.EditorAlias);
                pt.Setup(x => x.Variations).Returns(p.Variations);
                return pt.Object;
            }).ToArray();

            var contentType = new Mock<IContentType>();
            contentType.As<IContentTypeComposition>()
                .Setup(c => c.CompositionPropertyTypes)
                .Returns(propertyTypes);

            service.Setup(s => s.Get(key)).Returns(contentType.Object);
        }

        return service.Object;
    }
}

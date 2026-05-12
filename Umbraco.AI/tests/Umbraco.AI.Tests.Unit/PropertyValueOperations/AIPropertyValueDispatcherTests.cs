using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Umbraco.AI.Core.PropertyValueOperations;
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
    {
        var service = new Mock<IContentTypeService>();

        foreach (var (key, properties) in types)
        {
            var propertyTypes = properties.Select(p =>
            {
                var pt = new Mock<IPropertyType>();
                pt.Setup(x => x.Alias).Returns(p.Key);
                pt.Setup(x => x.PropertyEditorAlias).Returns(p.Value);
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

using System.Text.Json;
using System.Text.Json.Nodes;

using Moq;
using Shouldly;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class ContentPropertyValueOperationHelperTests
{
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock = new();
    private readonly Mock<IContentEditingService> _contentEditingServiceMock = new();
    private readonly Mock<IAIPropertyValueDispatcher> _dispatcherMock = new();
    private readonly Mock<IDataEditor> _editorMock = new();
    private readonly Mock<IDataValueEditor> _valueEditorMock = new();
    private readonly Mock<IJsonSerializer> _jsonSerializerMock = new();
    private readonly Mock<ITemplateService> _templateServiceMock = new();
    private readonly List<IDataEditor> _editors = [];

    public ContentPropertyValueOperationHelperTests()
    {
        _editorMock.Setup(x => x.Alias).Returns(TestEditorAlias);
        _editorMock.Setup(x => x.GetValueEditor()).Returns(_valueEditorMock.Object);
        _jsonSerializerMock.Setup(x => x.Serialize(It.IsAny<object?>())).Returns<object?>(v => JsonSerializer.Serialize(v));
    }

    private const string TestEditorAlias = "Test.Editor";

    private static readonly UmbracoPropertyPathSegmentArg RootSegment = new("contentBlocks", null);

    private static Mock<IContent> CreateContentMock(
        Guid contentTypeKey,
        object? currentValue,
        ContentVariation variation = ContentVariation.Nothing,
        IEnumerable<IProperty>? otherProperties = null)
    {
        var contentTypeMock = new Mock<ISimpleContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(contentTypeKey);
        contentTypeMock.Setup(x => x.Variations).Returns(variation);

        var contentMock = new Mock<IContent>();
        contentMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);
        contentMock.Setup(x => x.Name).Returns("Home");

        // The root value is read from the root property itself; add one unless the test supplies its own.
        var properties = (otherProperties ?? []).ToList();
        if (properties.All(p => p.Alias != RootSegment.Alias))
        {
            properties.Insert(0, CreatePropertyMock(RootSegment.Alias!, currentValue).Object);
        }

        contentMock.Setup(x => x.Properties).Returns(new PropertyCollection(properties));
        return contentMock;
    }

    private static Mock<IProperty> CreatePropertyMock(string alias, object? value, bool variesByCulture = false, bool variesBySegment = false, string? editorAlias = null)
    {
        var variations = ContentVariation.Nothing;
        if (variesByCulture)
        {
            variations |= ContentVariation.Culture;
        }

        if (variesBySegment)
        {
            variations |= ContentVariation.Segment;
        }

        // VariesByCulture()/VariesBySegment() are extension methods over Variations, not overridable
        // interface members, so Moq can't Setup() them directly — the underlying Variations property is
        // the real, mockable seam.
        var propertyTypeMock = new Mock<IPropertyType>();
        propertyTypeMock.Setup(x => x.Alias).Returns(alias);
        propertyTypeMock.Setup(x => x.Variations).Returns(variations);
        propertyTypeMock.Setup(x => x.PropertyEditorAlias).Returns(editorAlias ?? "Umbraco.Unknown");

        var propertyMock = new Mock<IProperty>();
        propertyMock.Setup(x => x.Alias).Returns(alias);
        propertyMock.Setup(x => x.PropertyType).Returns(propertyTypeMock.Object);
        propertyMock.Setup(x => x.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>())).Returns(value);
        return propertyMock;
    }

    private Task<ContentPropertyValueOperationOutcome> ExecuteAsync(
        Guid key,
        IReadOnlyList<UmbracoPropertyPathSegmentArg>? path,
        AIPropertyOperation operation = AIPropertyOperation.SetValue,
        JsonNode? args = null,
        string? culture = null,
        string? segment = null)
        => ContentPropertyValueOperationHelper.ExecuteAsync(
            _authorizerMock.Object,
            _contentEditingServiceMock.Object,
            _dispatcherMock.Object,
            new ContentEditorValueReader(
                new PropertyEditorCollection(new DataEditorCollection(() => _editors)),
                _jsonSerializerMock.Object),
            _templateServiceMock.Object,
            key,
            path,
            operation,
            args,
            culture,
            segment,
            CancellationToken.None);

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsErrorWithoutCallingAnything()
    {
        var result = await ExecuteAsync(Guid.Empty, [RootSegment]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("empty");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullPath_ReturnsError()
    {
        var result = await ExecuteAsync(Guid.NewGuid(), null);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Path must contain");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPath_ReturnsError()
    {
        var result = await ExecuteAsync(Guid.NewGuid(), []);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Path must contain");
    }

    [Fact]
    public async Task ExecuteAsync_RootSegmentIsBlockKey_ReturnsError()
    {
        var result = await ExecuteAsync(Guid.NewGuid(), [new UmbracoPropertyPathSegmentArg(null, Guid.NewGuid())]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("property alias segment");
    }

    [Fact]
    public async Task ExecuteAsync_MalformedNonRootSegment_ReturnsErrorWithoutAuthorizing()
    {
        // Neither Alias nor BlockKey set on the second segment.
        var result = await ExecuteAsync(Guid.NewGuid(), [RootSegment, new UmbracoPropertyPathSegmentArg(null, null)]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("exactly one");
        _authorizerMock.Verify(x => x.AuthorizeContentAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IEnumerable<string>?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutLoadingContent()
    {
        var key = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("no permission");
        _contentEditingServiceMock.Verify(x => x.GetAsync(It.IsAny<Guid>()), Times.Never);
        _dispatcherMock.Verify(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ContentNotFound_ReturnsError()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IContent?)null);

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_DispatcherFails_ReturnsMappedErrorWithoutPersisting()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateContentMock(contentTypeKey, null).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Fail(new AIPropertyValueOperationError(
                AIPropertyValueOperationError.Codes.OperationNotSupported, "Cannot add a block to a rich-text property.")));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("Cannot add a block to a rich-text property.");
        _contentEditingServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<ContentUpdateModel>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_PersistFails_ReturnsMappedMessage()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateContentMock(contentTypeKey, null).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Fail(
                ContentEditingOperationStatus.PropertyValidationError, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("validation");
    }

    [Fact]
    public async Task ExecuteAsync_RootValueMissing_DispatchesWithNullRootValue()
    {
        // Proves the "build from scratch" scenario: a property with no existing value dispatches
        // with RootValue: null, which is exactly what the block handlers build a fresh envelope from.
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateContentMock(contentTypeKey, null).Object);

        AIPropertyValueDispatchRequest? captured = null;
        var newBlockKey = Guid.NewGuid();
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonNode.Parse("""{"layout":{}}"""), newBlockKey));
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment], AIPropertyOperation.AddItem);

        result.Success.ShouldBeTrue();
        result.BlockKey.ShouldBe(newBlockKey);
        captured.ShouldNotBeNull();
        captured!.RootValue.ShouldBeNull();
        captured.DocumentMetadata.ContentTypeKey.ShouldBe(contentTypeKey);
        captured.DocumentMetadata.Variants.Single().ShouldBe(AIVariantId.Invariant);
    }

    [Fact]
    public async Task ExecuteAsync_PlainTextScalarCurrentValue_DoesNotThrowParsingAsJson()
    {
        // A text box's stored value is a plain, non-JSON string — must not throw when converted
        // to a JsonNode for the dispatcher.
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, "Hello World").Object);

        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("Hello World, updated")));
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.RootValue!.GetValue<string>().ShouldBe("Hello World");
    }

    [Fact]
    public async Task ExecuteAsync_ContentHasTemplate_KeepsItSoThePageStillRenders()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var templateKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        var contentMock = CreateContentMock(Guid.NewGuid(), "Hello World");
        contentMock.Setup(x => x.TemplateId).Returns(1234);
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);
        var templateMock = new Mock<ITemplate>();
        templateMock.Setup(x => x.Key).Returns(templateKey);
        _templateServiceMock.Setup(x => x.GetAsync(1234)).ReturnsAsync(templateMock.Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("Updated")));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        capturedModel!.TemplateKey.ShouldBe(templateKey);
    }

    [Fact]
    public async Task ExecuteAsync_NestedBlockPath_ParsesJsonEnvelopeAndBuildsCorrectSegments()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        var blockKey = Guid.NewGuid();
        const string envelope = """{"layout":{"Umbraco.BlockList":[]},"contentData":[],"settingsData":[],"expose":[]}""";
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, envelope).Object);

        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonNode.Parse(envelope)));
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var path = new UmbracoPropertyPathSegmentArg[]
        {
            new("contentBlocks", null),
            new(null, blockKey),
            new("innerText", null),
        };

        var result = await ExecuteAsync(key, path);

        result.Success.ShouldBeTrue();
        captured.ShouldNotBeNull();
        captured!.Path.Count.ShouldBe(3);
        captured.Path[0].ShouldBeOfType<AIPropertyPathSegment.PropertyAliasSegment>();
        var blockSegment = captured.Path[1].ShouldBeOfType<AIPropertyPathSegment.BlockKeySegment>();
        blockSegment.BlockKey.ShouldBe(blockKey);
        captured.Path[2].ShouldBeOfType<AIPropertyPathSegment.PropertyAliasSegment>();
        captured.RootValue.ShouldNotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_PersistsSinglePropertyValueModelForRootAlias()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateContentMock(contentTypeKey, null).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        var property = capturedModel!.Properties.Single();
        property.Alias.ShouldBe("contentBlocks");
        // Normalized via NormalizeIncomingValue (umbraco/Umbraco.AI#408) -- a JSON string arrives as a
        // plain CLR string, not a JsonElement, matching what the real backoffice save path produces.
        property.Value.ShouldBe("new value");

        // ContentEditingServiceBase.TryGetAndValidateContentType requires an invariant Variants entry
        // (Culture and Segment both null) for an invariant content type, or the whole update fails with
        // ContentTypeCultureVarianceMismatch even though nothing here changes the name.
        var variant = capturedModel.Variants.Single();
        variant.Name.ShouldBe("Home");
        variant.Culture.ShouldBeNull();
        variant.Segment.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ContentHasOtherProperties_ResubmitsTheirCurrentValuesSoTheySurvive()
    {
        // ContentEditingServiceBase.RemoveMissingProperties wipes any property alias not present in the
        // submitted set on every save. A property-value operation only ever names the root alias it's
        // operating on, so every other property here ("title", "employee") must come back out in the
        // captured model with its current value, or it's silently wiped by the save.
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var otherProperties = new List<IProperty>
        {
            CreatePropertyMock("title", "Old Title").Object,
            CreatePropertyMock("employee", "Old Employee").Object,
        };
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, null, otherProperties: otherProperties).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        capturedModel!.Properties.Count().ShouldBe(3);

        var root = capturedModel.Properties.Single(p => p.Alias == "contentBlocks");
        root.Value.ShouldBe("new value");

        var title = capturedModel.Properties.Single(p => p.Alias == "title");
        title.Value.ShouldBe("Old Title");

        var employee = capturedModel.Properties.Single(p => p.Alias == "employee");
        employee.Value.ShouldBe("Old Employee");
    }

    [Fact]
    public async Task ExecuteAsync_OtherPropertyVariesBySegment_IsLeftOutOfThePatch()
    {
        // There's no per-property segment to read/write these correctly here, so — matching
        // UpdateUmbracoContentTool — they're left out of the resubmitted set rather than risk a
        // NotSupportedException from guessing a segment.
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var otherProperties = new List<IProperty>
        {
            CreatePropertyMock("title", "Old Title").Object,
            CreatePropertyMock("regionalNote", "Old Note", variesBySegment: true).Object,
        };
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, null, otherProperties: otherProperties).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        capturedModel!.Properties.Select(p => p.Alias).ShouldBe(new[] { "contentBlocks", "title" });
    }

    [Fact]
    public async Task ExecuteAsync_InvariantRootPropertyOnVariantDocument_ReadsAndWritesRootWithNullCulture()
    {
        // An invariant block list on a culture-variant document: Property.GetValue returns null for a
        // culture on an invariant property, so reading with the edited culture would start the dispatch
        // from an empty value. The culture still reaches the dispatcher for nested block values.
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var rootPropertyMock = CreatePropertyMock("contentBlocks", null);
        var contentMock = CreateContentMock(
            contentTypeKey,
            null,
            ContentVariation.Culture,
            otherProperties: [rootPropertyMock.Object]);
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);

        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment], culture: "nl-NL");

        result.Success.ShouldBeTrue();
        rootPropertyMock.Verify(x => x.GetValue(null, null, false), Times.Once);
        captured!.DocumentMetadata.Variants.Single().Culture.ShouldBe("nl-NL");
        capturedModel!.Properties.Single(p => p.Alias == "contentBlocks").Culture.ShouldBeNull();
    }

    #region Values are read in editor format, culture is resolved, empty saves are caught

    private void AllowUpdate(Guid key, Guid userKey, Action<ContentUpdateModel>? capture = null, IContent? savedContent = null)
        => _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capture?.Invoke(model))
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult { Content = savedContent }));

    private (Guid Key, Guid UserKey, Guid ContentTypeKey) AllowAuthorization()
    {
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        return (key, userKey, Guid.NewGuid());
    }

    [Fact]
    public async Task ExecuteAsync_OtherPropertyHasEditor_ResubmitsEditorValueNotStoredValue()
    {
        // A dropdown stores "[\"primary\"]" (a JSON string); resubmitting that makes FromEditor drop it.
        // The value editor's ToEditor gives the shape the save path expects.
        var (key, userKey, contentTypeKey) = AllowAuthorization();
        _editors.Add(_editorMock.Object);
        var dropdown = CreatePropertyMock("theme", "[\"primary\"]", editorAlias: TestEditorAlias);
        _valueEditorMock
            .Setup(x => x.ToEditor(dropdown.Object, null, null))
            .Returns(new[] { "primary" });
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, null, otherProperties: [dropdown.Object]).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));
        ContentUpdateModel? capturedModel = null;
        AllowUpdate(key, userKey, m => capturedModel = m);

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        var theme = capturedModel!.Properties.Single(p => p.Alias == "theme");
        theme.Value.ShouldBeAssignableTo<IEnumerable<string>>()!.ShouldBe(["primary"]);
    }

    [Fact]
    public async Task ExecuteAsync_RootPropertyHasEditor_DispatchesFromEditorValue()
    {
        var (key, userKey, contentTypeKey) = AllowAuthorization();
        _editors.Add(_editorMock.Object);
        var root = CreatePropertyMock("contentBlocks", "stored", editorAlias: TestEditorAlias);
        _valueEditorMock
            .Setup(x => x.ToEditor(root.Object, null, null))
            .Returns(new { layout = new { } });
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, null, otherProperties: [root.Object]).Object);
        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));
        AllowUpdate(key, userKey);

        await ExecuteAsync(key, [RootSegment]);

        captured!.RootValue.ShouldBeOfType<JsonObject>().ContainsKey("layout").ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_VariantContentWithoutCulture_UsesTheOnlyCulture()
    {
        var (key, userKey, contentTypeKey) = AllowAuthorization();
        var contentMock = CreateContentMock(contentTypeKey, null, ContentVariation.Culture);
        contentMock.Setup(x => x.AvailableCultures).Returns(["en-US"]);
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);
        AIPropertyValueDispatchRequest? captured = null;
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AIPropertyValueDispatchRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));
        ContentUpdateModel? capturedModel = null;
        AllowUpdate(key, userKey, m => capturedModel = m);

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        captured!.DocumentMetadata.Variants.Single().Culture.ShouldBe("en-US");
        capturedModel!.Variants.Single().Culture.ShouldBe("en-US");
    }

    [Fact]
    public async Task ExecuteAsync_VariantContentWithoutCultureAndSeveralCultures_AsksForCulture()
    {
        var (key, _, contentTypeKey) = AllowAuthorization();
        var contentMock = CreateContentMock(contentTypeKey, null, ContentVariation.Culture);
        contentMock.Setup(x => x.AvailableCultures).Returns(["en-US", "da-DK"]);
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(contentMock.Object);

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Culture is required");
        result.Message.ShouldContain("en-US, da-DK");
        _dispatcherMock.Verify(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_SetValueSavedAsEmpty_ReturnsErrorAndRestoresPreviousValue()
    {
        // Rich text sent in the wrong shape passes validation but is stored as null.
        var (key, userKey, contentTypeKey) = AllowAuthorization();
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, "Old intro").Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonNode.Parse("""{"blocks":[{"tag":"p"}]}""")));
        JsonNode? ignored = null;
        _dispatcherMock.As<IAIPropertyValueReader>()
            .Setup(x => x.TryReadValue(It.IsAny<AIPropertyValueDispatchRequest>(), out ignored))
            .Returns(true);
        var savedModels = new List<object?>();
        AllowUpdate(
            key,
            userKey,
            m => savedModels.Add(m.Properties.Single(p => p.Alias == "contentBlocks").Value),
            CreateContentMock(contentTypeKey, null).Object);

        var result = await ExecuteAsync(key, [RootSegment], args: new JsonObject { ["value"] = JsonNode.Parse("""{"blocks":[{"tag":"p"}]}""") });

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("was not saved");
        result.Message.ShouldContain("previous value has been kept");
        savedModels.Count.ShouldBe(2);
        savedModels[1].ShouldBe("Old intro");
    }

    [Fact]
    public async Task ExecuteAsync_SetValueSaved_DoesNotSaveAgain()
    {
        var (key, userKey, contentTypeKey) = AllowAuthorization();
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, "Old intro").Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("New intro")));
        JsonNode? saved = JsonValue.Create("New intro");
        _dispatcherMock.As<IAIPropertyValueReader>()
            .Setup(x => x.TryReadValue(It.IsAny<AIPropertyValueDispatchRequest>(), out saved))
            .Returns(true);
        AllowUpdate(key, userKey, savedContent: CreateContentMock(contentTypeKey, "New intro").Object);

        var result = await ExecuteAsync(key, [RootSegment], args: new JsonObject { ["value"] = "New intro" });

        result.Success.ShouldBeTrue();
        _contentEditingServiceMock.Verify(
            x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey),
            Times.Once);
    }

    #endregion

    #region umbraco/Umbraco.AI#408 -- JsonElement values must be normalized before reaching FromEditor

    [Fact]
    public async Task ExecuteAsync_DispatcherReturnsJsonBoolean_NormalizesToClrBoolNotJsonElement()
    {
        // Umbraco.TrueFalse's FromEditor pattern-matches on bool/int/string -- an un-normalized JsonElement
        // matches none of those and silently coerces to false, even though the dispatcher said "true".
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _contentEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateContentMock(contentTypeKey, 0).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create(true)));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        var property = capturedModel!.Properties.Single(p => p.Alias == "contentBlocks");
        property.Value.ShouldBeOfType<bool>();
        property.Value.ShouldBe(true);
    }

    [Fact]
    public async Task ExecuteAsync_OtherPropertyStoresTrueFalseValueOne_ResubmitsAsClrIntNotJsonElement()
    {
        // Reproduces the latent half of umbraco/Umbraco.AI#408: a TrueFalse property currently storing
        // `1` (true) gets resubmitted here as an untouched sibling on every save. Before the fix, the
        // round-tripped value stayed a JsonElement -- which FromEditor's bool/int/string switch doesn't
        // match -- so ANY save through these tools would silently flip an untouched `true` toggle back
        // to `false`, not just an explicit write to it.
        var key = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var contentTypeKey = Guid.NewGuid();
        _authorizerMock
            .Setup(x => x.AuthorizeContentAsync(ActionUpdate.ActionLetter, key, null))
            .ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));

        var otherProperties = new List<IProperty> { CreatePropertyMock("isFeatured", 1).Object };
        _contentEditingServiceMock
            .Setup(x => x.GetAsync(key))
            .ReturnsAsync(CreateContentMock(contentTypeKey, null, otherProperties: otherProperties).Object);
        _dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<AIPropertyValueDispatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AIPropertyValueDispatchResult.Ok(JsonValue.Create("new value")));

        ContentUpdateModel? capturedModel = null;
        _contentEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<ContentUpdateModel>(), userKey))
            .Callback<Guid, ContentUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<ContentUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new ContentUpdateResult()));

        var result = await ExecuteAsync(key, [RootSegment]);

        result.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        var isFeatured = capturedModel!.Properties.Single(p => p.Alias == "isFeatured");
        isFeatured.Value.ShouldBeOfType<int>();
        isFeatured.Value.ShouldBe(1);
    }

    [Fact]
    public void NormalizeIncomingValue_JsonArrayOfObjects_ReturnsJsonArrayNotJsonElement()
    {
        // Umbraco.MultiNodeTreePicker's FromEditor requires `editorValue.Value is JsonArray` -- an
        // un-normalized JsonElement array never satisfies that type check either, and the picker silently
        // clears instead of erroring.
        var value = JsonDocument.Parse("""[{"type":"document","unique":"11111111-1111-1111-1111-111111111111"}]""").RootElement;

        var normalized = ContentPropertyValueOperationHelper.NormalizeIncomingValue(value);

        normalized.ShouldBeOfType<JsonArray>();
        ((JsonArray)normalized!).Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("42", 42)]
    [InlineData("\"Hello World\"", "Hello World")]
    public void NormalizeIncomingValue_ScalarShapes_ReturnClrPrimitivesNotJsonElement(string json, object expected)
    {
        var value = JsonDocument.Parse(json).RootElement;

        var normalized = ContentPropertyValueOperationHelper.NormalizeIncomingValue(value);

        normalized.ShouldBe(expected);
    }

    #endregion
}

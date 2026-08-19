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
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class ContentPropertyValueOperationHelperTests
{
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock = new();
    private readonly Mock<IContentEditingService> _contentEditingServiceMock = new();
    private readonly Mock<IAIPropertyValueDispatcher> _dispatcherMock = new();

    private static readonly UmbracoPropertyPathSegmentArg RootSegment = new("contentBlocks", null);

    private static Mock<IContent> CreateContentMock(Guid contentTypeKey, object? currentValue, ContentVariation variation = ContentVariation.Nothing)
    {
        var contentTypeMock = new Mock<ISimpleContentType>();
        contentTypeMock.Setup(x => x.Key).Returns(contentTypeKey);
        contentTypeMock.Setup(x => x.Variations).Returns(variation);

        var contentMock = new Mock<IContent>();
        contentMock.Setup(x => x.ContentType).Returns(contentTypeMock.Object);
        contentMock.Setup(x => x.Name).Returns("Home");
        contentMock.Setup(x => x.GetValue(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), false)).Returns(currentValue);
        return contentMock;
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
        ((JsonElement)property.Value!).GetString().ShouldBe("new value");

        // ContentEditingServiceBase.TryGetAndValidateContentType requires an invariant Variants entry
        // (Culture and Segment both null) for an invariant content type, or the whole update fails with
        // ContentTypeCultureVarianceMismatch even though nothing here changes the name.
        var variant = capturedModel.Variants.Single();
        variant.Name.ShouldBe("Home");
        variant.Culture.ShouldBeNull();
        variant.Segment.ShouldBeNull();
    }
}

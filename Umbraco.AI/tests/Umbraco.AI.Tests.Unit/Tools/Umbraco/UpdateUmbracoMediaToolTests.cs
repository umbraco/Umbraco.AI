using System.Text.Json;

using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class UpdateUmbracoMediaToolTests
{
    private readonly Mock<IMediaEditingService> _mediaEditingServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public UpdateUmbracoMediaToolTests()
    {
        _mediaEditingServiceMock = new Mock<IMediaEditingService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new UpdateUmbracoMediaTool(_mediaEditingServiceMock.Object, _authorizerMock.Object, new PropertyEditorCollection(new DataEditorCollection(() => [])), Mock.Of<IJsonSerializer>());
    }

    private static Mock<IMedia> CreateMediaMock(Guid key, string name, string mediaTypeAlias, IEnumerable<IProperty>? properties = null)
    {
        var mediaTypeMock = new Mock<ISimpleContentType>();
        mediaTypeMock.Setup(x => x.Alias).Returns(mediaTypeAlias);

        var mediaMock = new Mock<IMedia>();
        mediaMock.Setup(x => x.Key).Returns(key);
        mediaMock.Setup(x => x.Name).Returns(name);
        mediaMock.Setup(x => x.ContentType).Returns(mediaTypeMock.Object);
        mediaMock.Setup(x => x.CreateDate).Returns(DateTime.UtcNow);
        mediaMock.Setup(x => x.UpdateDate).Returns(DateTime.UtcNow);
        mediaMock.Setup(x => x.Properties).Returns(new PropertyCollection(properties ?? []));
        return mediaMock;
    }

    private static IProperty CreatePropertyMock(string alias, object? value)
    {
        var propertyTypeMock = new Mock<IPropertyType>();
        propertyTypeMock.Setup(x => x.Alias).Returns(alias);
        propertyTypeMock.Setup(x => x.PropertyEditorAlias).Returns("Umbraco.Unknown");

        var propertyMock = new Mock<IProperty>();
        propertyMock.Setup(x => x.Alias).Returns(alias);
        propertyMock.Setup(x => x.PropertyType).Returns(propertyTypeMock.Object);
        propertyMock.Setup(x => x.GetValue(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>())).Returns(value);
        return propertyMock.Object;
    }

    [Fact]
    public async Task ExecuteAsync_RenameOnly_SendsBackEveryPropertySoTheFileSurvives()
    {
        // The CMS clears every property missing from an update, so a rename that sent no properties
        // emptied umbracoFile and the media item lost its file.
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        var existing = CreateMediaMock(key, "Logo", "image", [CreatePropertyMock("umbracoFile", "/media/abc/logo.png"), CreatePropertyMock("altText", "Our logo")]);
        _mediaEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(existing.Object);

        MediaUpdateModel? capturedModel = null;
        _mediaEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<MediaUpdateModel>(), userKey))
            .Callback<Guid, MediaUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<MediaUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new MediaUpdateResult { Content = existing.Object }));

        await _tool.ExecuteAsync(new UpdateUmbracoMediaArgs(key, "New logo", null), CancellationToken.None);

        capturedModel.ShouldNotBeNull();
        capturedModel!.Properties.Select(p => p.Alias).ShouldBe(new[] { "umbracoFile", "altText" });
        capturedModel.Properties.Single(p => p.Alias == "umbracoFile").Value.ShouldBe("/media/abc/logo.png");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyKey_ReturnsError()
    {
        var args = new UpdateUmbracoMediaArgs(Guid.Empty, "New Name", null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        _authorizerMock.Verify(x => x.AuthorizeMediaAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingService()
    {
        var key = Guid.NewGuid();
        var args = new UpdateUmbracoMediaArgs(key, "New Name", null);
        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _mediaEditingServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<MediaUpdateModel>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_UpdatesWithResolvedUserKeyAndReturnsMedia()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement> { ["altText"] = JsonDocument.Parse("\"Updated\"").RootElement };
        var args = new UpdateUmbracoMediaArgs(key, "New Name", propertyValues);

        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _mediaEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateMediaMock(key, "Old Name", "image").Object);
        var updatedMediaMock = CreateMediaMock(key, "New Name", "image");

        MediaUpdateModel? capturedModel = null;
        _mediaEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<MediaUpdateModel>(), userKey))
            .Callback<Guid, MediaUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<MediaUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new MediaUpdateResult { Content = updatedMediaMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoMediaResult>();
        typed.Success.ShouldBeTrue();
        typed.Media!.Key.ShouldBe(key);
        capturedModel.ShouldNotBeNull();
        capturedModel!.Variants.Single().Name.ShouldBe("New Name");
        capturedModel.Properties.Single().Alias.ShouldBe("altText");
    }

    [Fact]
    public async Task ExecuteAsync_NoNameChange_LoadsCurrentNameToAvoidCultureVarianceMismatch()
    {
        // ContentEditingServiceBase (shared with media) requires a Variants entry matching the media
        // type's variance regardless of what's being changed — an invariant type with an empty Variants
        // list fails with ContentTypeCultureVarianceMismatch even though nothing here is renaming the
        // item. When no new name is given, the tool must load the current one so a matching entry is
        // still present.
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement> { ["altText"] = JsonDocument.Parse("\"Updated\"").RootElement };
        var args = new UpdateUmbracoMediaArgs(key, null, propertyValues);

        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _mediaEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync(CreateMediaMock(key, "Existing Name", "image").Object);

        MediaUpdateModel? capturedModel = null;
        var updatedMediaMock = CreateMediaMock(key, "Existing Name", "image");
        _mediaEditingServiceMock
            .Setup(x => x.UpdateAsync(key, It.IsAny<MediaUpdateModel>(), userKey))
            .Callback<Guid, MediaUpdateModel, Guid>((_, model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<MediaUpdateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new MediaUpdateResult { Content = updatedMediaMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoMediaResult>();
        typed.Success.ShouldBeTrue();
        capturedModel.ShouldNotBeNull();
        capturedModel!.Variants.Single().Name.ShouldBe("Existing Name");
    }

    [Fact]
    public async Task ExecuteAsync_NoNameChange_MediaNotFound_ReturnsErrorWithoutCallingUpdate()
    {
        var userKey = Guid.NewGuid();
        var key = Guid.NewGuid();
        var args = new UpdateUmbracoMediaArgs(key, null, null);

        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(key)).ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        _mediaEditingServiceMock.Setup(x => x.GetAsync(key)).ReturnsAsync((IMedia?)null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<UpdateUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldContain("not found");
        _mediaEditingServiceMock.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<MediaUpdateModel>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
    }
}

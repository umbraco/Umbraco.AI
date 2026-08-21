using System.Text.Json;

using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class CreateUmbracoMediaToolTests
{
    private readonly Mock<IMediaEditingService> _mediaEditingServiceMock;
    private readonly Mock<IMediaTypeService> _mediaTypeServiceMock;
    private readonly Mock<IMediaService> _mediaServiceMock;
    private readonly Mock<IUmbracoWriteAuthorizer> _authorizerMock;
    private readonly IAITool _tool;

    public CreateUmbracoMediaToolTests()
    {
        _mediaEditingServiceMock = new Mock<IMediaEditingService>();
        _mediaTypeServiceMock = new Mock<IMediaTypeService>();
        _mediaServiceMock = new Mock<IMediaService>();
        _authorizerMock = new Mock<IUmbracoWriteAuthorizer>();
        _tool = new CreateUmbracoMediaTool(_mediaEditingServiceMock.Object, _mediaTypeServiceMock.Object, _mediaServiceMock.Object, _authorizerMock.Object);
    }

    private static Mock<IMedia> CreateMediaMock(Guid key, string name, string mediaTypeAlias)
    {
        var mediaTypeMock = new Mock<ISimpleContentType>();
        mediaTypeMock.Setup(x => x.Alias).Returns(mediaTypeAlias);

        var mediaMock = new Mock<IMedia>();
        mediaMock.Setup(x => x.Key).Returns(key);
        mediaMock.Setup(x => x.Name).Returns(name);
        mediaMock.Setup(x => x.ContentType).Returns(mediaTypeMock.Object);
        mediaMock.Setup(x => x.CreateDate).Returns(DateTime.UtcNow);
        mediaMock.Setup(x => x.UpdateDate).Returns(DateTime.UtcNow);
        mediaMock.Setup(x => x.Properties).Returns(new PropertyCollection());
        return mediaMock;
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyMediaTypeAlias_ReturnsError()
    {
        var args = new CreateUmbracoMediaArgs(null, "", "Logo", null);

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        _authorizerMock.Verify(x => x.AuthorizeMediaAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AuthorizationDenied_ReturnsErrorWithoutCallingServices()
    {
        var args = new CreateUmbracoMediaArgs(null, "image", "Logo", null);
        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(null)).ReturnsAsync(UmbracoWriteAuthorizationResult.Denied("no permission"));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoMediaResult>();
        typed.Success.ShouldBeFalse();
        typed.Message.ShouldBe("no permission");
        _mediaTypeServiceMock.Verify(x => x.Get(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_HappyPath_CreatesWithResolvedUserKeyAndReturnsMedia()
    {
        var userKey = Guid.NewGuid();
        var mediaTypeKey = Guid.NewGuid();
        var newKey = Guid.NewGuid();
        var propertyValues = new Dictionary<string, JsonElement> { ["altText"] = JsonDocument.Parse("\"Logo\"").RootElement };
        var args = new CreateUmbracoMediaArgs(null, "image", "Logo", propertyValues);

        _authorizerMock.Setup(x => x.AuthorizeMediaAsync(null)).ReturnsAsync(UmbracoWriteAuthorizationResult.Allowed(userKey));
        var mediaTypeMock = new Mock<IMediaType>();
        mediaTypeMock.Setup(x => x.Key).Returns(mediaTypeKey);
        _mediaTypeServiceMock.Setup(x => x.Get("image")).Returns(mediaTypeMock.Object);

        var createdMediaMock = CreateMediaMock(newKey, "Logo", "image");
        MediaCreateModel? capturedModel = null;
        _mediaEditingServiceMock
            .Setup(x => x.CreateAsync(It.IsAny<MediaCreateModel>(), userKey))
            .Callback<MediaCreateModel, Guid>((model, _) => capturedModel = model)
            .ReturnsAsync(Attempt<MediaCreateResult, ContentEditingOperationStatus>.Succeed(
                ContentEditingOperationStatus.Success, new MediaCreateResult { Content = createdMediaMock.Object }));

        var result = await _tool.ExecuteAsync(args, CancellationToken.None);

        var typed = result.ShouldBeOfType<CreateUmbracoMediaResult>();
        typed.Success.ShouldBeTrue();
        typed.Media!.Key.ShouldBe(newKey);
        capturedModel.ShouldNotBeNull();
        capturedModel!.ContentTypeKey.ShouldBe(mediaTypeKey);
        capturedModel.Variants.Single().Name.ShouldBe("Logo");
        capturedModel.Properties.Single().Alias.ShouldBe("altText");
    }

    [Fact]
    public void Description_ReturnsNonEmptyString()
    {
        var description = _tool.Description;

        description.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DescribeInvocation_NoParentKey_DescribesCreatingAtTheRoot()
    {
        var args = new CreateUmbracoMediaArgs(null, "image", "Logo", null);

        var description = _tool.DescribeInvocation(args);

        description.ShouldBe("Create a new 'image' media item named 'Logo' at the root.");
    }

    [Fact]
    public void DescribeInvocation_ParentResolves_UsesItsNameInsteadOfTheRawKey()
    {
        var parentKey = Guid.NewGuid();
        _mediaServiceMock.Setup(x => x.GetById(parentKey)).Returns(Mock.Of<IMedia>(m => m.Name == "Images"));
        var args = new CreateUmbracoMediaArgs(parentKey, "image", "Logo", null);

        var description = _tool.DescribeInvocation(args);

        description.ShouldBe("Create a new 'image' media item named 'Logo' under parent 'Images'.");
    }

    [Fact]
    public void DescribeInvocation_ParentDoesNotResolve_FallsBackToTheRawKey()
    {
        var parentKey = Guid.NewGuid();
        _mediaServiceMock.Setup(x => x.GetById(parentKey)).Returns((IMedia?)null);
        var args = new CreateUmbracoMediaArgs(parentKey, "image", "Logo", null);

        var description = _tool.DescribeInvocation(args);

        description.ShouldBe($"Create a new 'image' media item named 'Logo' under parent {parentKey}.");
    }
}

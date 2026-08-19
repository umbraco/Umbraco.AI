using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class UmbracoWriteAuthorizerTests
{
    private readonly Mock<IContentPermissionAuthorizer> _contentPermissionAuthorizerMock;
    private readonly Mock<IMediaPermissionAuthorizer> _mediaPermissionAuthorizerMock;
    private readonly Mock<IBackOfficeSecurityAccessor> _backOfficeSecurityAccessorMock;
    private readonly IUmbracoWriteAuthorizer _authorizer;

    public UmbracoWriteAuthorizerTests()
    {
        _contentPermissionAuthorizerMock = new Mock<IContentPermissionAuthorizer>();
        _mediaPermissionAuthorizerMock = new Mock<IMediaPermissionAuthorizer>();
        _backOfficeSecurityAccessorMock = new Mock<IBackOfficeSecurityAccessor>();
        _authorizer = new UmbracoWriteAuthorizer(
            _contentPermissionAuthorizerMock.Object,
            _mediaPermissionAuthorizerMock.Object,
            _backOfficeSecurityAccessorMock.Object);
    }

    private void SetCurrentUser(IUser? user)
    {
        var securityMock = new Mock<IBackOfficeSecurity>();
        securityMock.Setup(x => x.CurrentUser).Returns(user);
        _backOfficeSecurityAccessorMock.Setup(x => x.BackOfficeSecurity).Returns(securityMock.Object);
    }

    private static Mock<IUser> CreateUserMock(Guid key)
    {
        var userMock = new Mock<IUser>();
        userMock.Setup(x => x.Key).Returns(key);
        return userMock;
    }

    [Fact]
    public async Task AuthorizeContentAsync_NoCurrentUser_ReturnsDenied()
    {
        SetCurrentUser(null);

        var result = await _authorizer.AuthorizeContentAsync("Umb.Document.Update", Guid.NewGuid());

        result.IsAuthorized.ShouldBeFalse();
        result.UserKey.ShouldBeNull();
        _contentPermissionAuthorizerMock.Verify(
            x => x.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthorizeContentAsync_NullContentKey_ChecksRootLevel()
    {
        var user = CreateUserMock(Guid.NewGuid());
        SetCurrentUser(user.Object);
        _contentPermissionAuthorizerMock
            .Setup(x => x.IsDeniedAtRootLevelAsync(user.Object, It.Is<ISet<string>>(s => s.Contains("Umb.Document.Create"))))
            .ReturnsAsync(false);

        var result = await _authorizer.AuthorizeContentAsync("Umb.Document.Create", null);

        result.IsAuthorized.ShouldBeTrue();
        result.UserKey.ShouldBe(user.Object.Key);
        _contentPermissionAuthorizerMock.Verify(
            x => x.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthorizeContentAsync_KeyedContentDenied_ReturnsDenied()
    {
        var user = CreateUserMock(Guid.NewGuid());
        var contentKey = Guid.NewGuid();
        SetCurrentUser(user.Object);
        _contentPermissionAuthorizerMock
            .Setup(x => x.IsDeniedAsync(user.Object, contentKey, "Umb.Document.Update"))
            .ReturnsAsync(true);

        var result = await _authorizer.AuthorizeContentAsync("Umb.Document.Update", contentKey);

        result.IsAuthorized.ShouldBeFalse();
        result.UserKey.ShouldBeNull();
    }

    [Fact]
    public async Task AuthorizeContentAsync_CulturesDenied_ShortCircuitsEvenWhenBaseCheckPasses()
    {
        var user = CreateUserMock(Guid.NewGuid());
        var contentKey = Guid.NewGuid();
        SetCurrentUser(user.Object);
        _contentPermissionAuthorizerMock
            .Setup(x => x.IsDeniedAsync(user.Object, contentKey, "Umb.Document.Publish"))
            .ReturnsAsync(false);
        _contentPermissionAuthorizerMock
            .Setup(x => x.IsDeniedForCultures(user.Object, It.Is<ISet<string>>(s => s.Contains("da-DK"))))
            .ReturnsAsync(true);

        var result = await _authorizer.AuthorizeContentAsync("Umb.Document.Publish", contentKey, ["da-DK"]);

        result.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeContentAsync_NoCulturesRequested_SkipsCultureCheck()
    {
        var user = CreateUserMock(Guid.NewGuid());
        var contentKey = Guid.NewGuid();
        SetCurrentUser(user.Object);
        _contentPermissionAuthorizerMock
            .Setup(x => x.IsDeniedAsync(user.Object, contentKey, "Umb.Document.Update"))
            .ReturnsAsync(false);

        var result = await _authorizer.AuthorizeContentAsync("Umb.Document.Update", contentKey);

        result.IsAuthorized.ShouldBeTrue();
        result.UserKey.ShouldBe(user.Object.Key);
        _contentPermissionAuthorizerMock.Verify(
            x => x.IsDeniedForCultures(It.IsAny<IUser>(), It.IsAny<ISet<string>>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthorizeMediaAsync_NoCurrentUser_ReturnsDenied()
    {
        SetCurrentUser(null);

        var result = await _authorizer.AuthorizeMediaAsync(Guid.NewGuid());

        result.IsAuthorized.ShouldBeFalse();
        _mediaPermissionAuthorizerMock.Verify(x => x.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AuthorizeMediaAsync_NullMediaKey_ChecksRootLevel()
    {
        var user = CreateUserMock(Guid.NewGuid());
        SetCurrentUser(user.Object);
        _mediaPermissionAuthorizerMock.Setup(x => x.IsDeniedAtRootLevelAsync(user.Object)).ReturnsAsync(false);

        var result = await _authorizer.AuthorizeMediaAsync(null);

        result.IsAuthorized.ShouldBeTrue();
        result.UserKey.ShouldBe(user.Object.Key);
        _mediaPermissionAuthorizerMock.Verify(x => x.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AuthorizeMediaAsync_KeyedMediaDenied_ReturnsDenied()
    {
        var user = CreateUserMock(Guid.NewGuid());
        var mediaKey = Guid.NewGuid();
        SetCurrentUser(user.Object);
        _mediaPermissionAuthorizerMock.Setup(x => x.IsDeniedAsync(user.Object, mediaKey)).ReturnsAsync(true);

        var result = await _authorizer.AuthorizeMediaAsync(mediaKey);

        result.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public async Task AuthorizeMediaAsync_KeyedMediaAllowed_ReturnsUserKey()
    {
        var user = CreateUserMock(Guid.NewGuid());
        var mediaKey = Guid.NewGuid();
        SetCurrentUser(user.Object);
        _mediaPermissionAuthorizerMock.Setup(x => x.IsDeniedAsync(user.Object, mediaKey)).ReturnsAsync(false);

        var result = await _authorizer.AuthorizeMediaAsync(mediaKey);

        result.IsAuthorized.ShouldBeTrue();
        result.UserKey.ShouldBe(user.Object.Key);
    }
}

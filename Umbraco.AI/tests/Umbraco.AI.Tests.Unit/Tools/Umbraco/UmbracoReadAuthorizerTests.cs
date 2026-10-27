using Moq;
using Shouldly;
using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

/// <summary>
/// The read authorizer applies the CMS's two steps: the section for the kind of access, then the
/// per-item start-node and permission check.
/// </summary>
public class UmbracoReadAuthorizerTests
{
    // Content tree: Home (1050) > Team Area (1100, the start node) > Team Page (1110); Home > Board (1200).
    private const string HomePath = "-1,1050";
    private const string TeamAreaPath = "-1,1050,1100";
    private const string TeamPagePath = "-1,1050,1100,1110";
    private const string BoardPath = "-1,1050,1200";

    private readonly Mock<IContentPermissionAuthorizer> _contentAuthorizer = new();
    private readonly Mock<IMediaPermissionAuthorizer> _mediaAuthorizer = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessor = new();
    private readonly Mock<IEntityService> _entityService = new();
    private readonly UmbracoReadAuthorizer _authorizer;

    public UmbracoReadAuthorizerTests()
    {
        _entityService
            .Setup(x => x.GetAllPaths(UmbracoObjectTypes.Document, It.IsAny<int[]>()))
            .Returns([new TreeEntityPath { Id = 1100, Path = TeamAreaPath }]);

        _authorizer = new UmbracoReadAuthorizer(
            _contentAuthorizer.Object,
            _mediaAuthorizer.Object,
            _securityAccessor.Object,
            _entityService.Object,
            AppCaches.NoCache);
    }

    [Fact]
    public async Task NoActingUser_DeniesEveryRead()
    {
        WithUser(null);

        (await _authorizer.CanReadContentAsync(Guid.NewGuid())).ShouldBeFalse();
        (await _authorizer.CanSeeContentInTreeAsync(Guid.NewGuid(), TeamPagePath)).ShouldBeFalse();
        (await _authorizer.CanReadMediaAsync(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public async Task CanReadContent_WithContentSectionAndBrowse_IsAllowed()
    {
        var user = WithUser(User("content"));
        var key = Guid.NewGuid();
        _contentAuthorizer.Setup(x => x.IsDeniedAsync(user, key, ActionBrowse.ActionLetter)).ReturnsAsync(false);

        (await _authorizer.CanReadContentAsync(key)).ShouldBeTrue();
    }

    [Fact]
    public async Task CanReadContent_WithoutBrowse_IsDenied()
    {
        var user = WithUser(User("content"));
        var key = Guid.NewGuid();
        _contentAuthorizer.Setup(x => x.IsDeniedAsync(user, key, ActionBrowse.ActionLetter)).ReturnsAsync(true);

        (await _authorizer.CanReadContentAsync(key)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("settings")]
    [InlineData("media")]
    [InlineData("translation")]
    public async Task CanReadContent_WithoutContentSection_IsDenied(string section)
    {
        var user = WithUser(User(section));
        _contentAuthorizer.Setup(x => x.IsDeniedAsync(user, It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(false);

        (await _authorizer.CanReadContentAsync(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public async Task CanSeeInTree_ItemUnderStartNode_NeedsBrowse()
    {
        var user = WithUser(User("settings"));
        var allowed = Guid.NewGuid();
        var denied = Guid.NewGuid();
        _contentAuthorizer.Setup(x => x.IsDeniedAsync(user, allowed, ActionBrowse.ActionLetter)).ReturnsAsync(false);
        _contentAuthorizer.Setup(x => x.IsDeniedAsync(user, denied, ActionBrowse.ActionLetter)).ReturnsAsync(true);

        (await _authorizer.CanSeeContentInTreeAsync(allowed, TeamPagePath)).ShouldBeTrue();
        (await _authorizer.CanSeeContentInTreeAsync(denied, TeamPagePath)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanSeeInTree_AncestorOfStartNode_IsShown()
    {
        WithUser(User("content"));

        (await _authorizer.CanSeeContentInTreeAsync(Guid.NewGuid(), HomePath)).ShouldBeTrue();
        _contentAuthorizer.Verify(x => x.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CanSeeInTree_OutsideStartNodeBranch_IsHidden()
    {
        WithUser(User("content"));

        (await _authorizer.CanSeeContentInTreeAsync(Guid.NewGuid(), BoardPath)).ShouldBeFalse();
    }

    [Fact]
    public async Task CanSeeInTree_WithoutAnyTreeSection_IsHidden()
    {
        WithUser(User("translation"));

        (await _authorizer.CanSeeContentInTreeAsync(Guid.NewGuid(), TeamPagePath)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("content")]
    [InlineData("media")]
    [InlineData("settings")]
    public async Task CanReadMedia_WithAnyTreeSection_IsAllowed(string section)
    {
        var user = WithUser(User(section));
        _mediaAuthorizer.Setup(x => x.IsDeniedAsync(user, It.IsAny<Guid>())).ReturnsAsync(false);

        (await _authorizer.CanReadMediaAsync(Guid.NewGuid())).ShouldBeTrue();
    }

    [Fact]
    public async Task CanReadMedia_OutsideMediaAccess_IsDenied()
    {
        var user = WithUser(User("media"));
        _mediaAuthorizer.Setup(x => x.IsDeniedAsync(user, It.IsAny<Guid>())).ReturnsAsync(true);

        (await _authorizer.CanReadMediaAsync(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public async Task CanReadMedia_WithoutAnyTreeSection_IsDenied()
    {
        var user = WithUser(User("translation"));
        _mediaAuthorizer.Setup(x => x.IsDeniedAsync(user, It.IsAny<Guid>())).ReturnsAsync(false);

        (await _authorizer.CanReadMediaAsync(Guid.NewGuid())).ShouldBeFalse();
    }

    private static IUser User(params string[] sections)
    {
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Key).Returns(Guid.NewGuid());
        user.SetupGet(x => x.AllowedSections).Returns(sections);
        user.SetupGet(x => x.StartContentIds).Returns([1100]);
        user.SetupGet(x => x.Groups).Returns([]);
        return user.Object;
    }

    private IUser? WithUser(IUser? user)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(x => x.CurrentUser).Returns(user);
        _securityAccessor.SetupGet(x => x.BackOfficeSecurity).Returns(security.Object);
        return user;
    }
}

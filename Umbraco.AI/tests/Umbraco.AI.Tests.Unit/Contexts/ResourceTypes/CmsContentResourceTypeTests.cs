using Moq;
using Shouldly;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Services;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Contexts.ResourceTypes;

/// <summary>
/// Security-focused tests for <see cref="CmsContentResourceType"/>: live content must only be resolved
/// for an authenticated acting user who has read permission on the node (decision #6 / F-SEC).
/// </summary>
public class CmsContentResourceTypeTests
{
    private readonly Mock<IContentService> _contentService = new();
    private readonly Mock<IContentPermissionAuthorizer> _authorizer = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessor = new();
    private readonly IUser _user = new Mock<IUser>().Object;

    private CmsContentResourceType CreateSut()
        => new(new Mock<IAIContextResourceTypeInfrastructure>().Object, _contentService.Object, _authorizer.Object, _securityAccessor.Object);

    private void WithActingUser(IUser? user)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(s => s.CurrentUser).Returns(user);
        _securityAccessor.SetupGet(a => a.BackOfficeSecurity).Returns(user is null ? null : security.Object);
    }

    [Fact]
    public async Task InvalidContentId_ReturnsNull_WithoutTouchingServices()
    {
        var result = await CreateSut().ResolveDataAsync(new CmsContentResourceSettings { ContentId = "not-a-guid" });

        result.ShouldBeNull();
        _authorizer.Verify(a => a.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _contentService.Verify(c => c.GetById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task NoActingUser_ReturnsNull_WithoutPermissionCheckOrFetch()
    {
        WithActingUser(null);

        var result = await CreateSut().ResolveDataAsync(new CmsContentResourceSettings { ContentId = Guid.NewGuid().ToString() });

        result.ShouldBeNull();
        _authorizer.Verify(a => a.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _contentService.Verify(c => c.GetById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task PermissionDenied_ReturnsNull_WithoutFetchingContent()
    {
        var key = Guid.NewGuid();
        WithActingUser(_user);
        _authorizer.Setup(a => a.IsDeniedAsync(_user, key, ActionBrowse.ActionLetter)).ReturnsAsync(true);

        var result = await CreateSut().ResolveDataAsync(new CmsContentResourceSettings { ContentId = key.ToString() });

        result.ShouldBeNull();
        _contentService.Verify(c => c.GetById(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task PermissionAllowed_ReturnsContentSnapshot()
    {
        var key = Guid.NewGuid();
        WithActingUser(_user);
        _authorizer.Setup(a => a.IsDeniedAsync(_user, key, ActionBrowse.ActionLetter)).ReturnsAsync(false);

        var content = new Mock<IContent>();
        content.SetupGet(c => c.Name).Returns("Home");
        content.SetupGet(c => c.Properties).Returns(new PropertyCollection());
        _contentService.Setup(c => c.GetById(key)).Returns(content.Object);

        var result = await CreateSut().ResolveDataAsync(new CmsContentResourceSettings { ContentId = key.ToString() });

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Home");
    }
}

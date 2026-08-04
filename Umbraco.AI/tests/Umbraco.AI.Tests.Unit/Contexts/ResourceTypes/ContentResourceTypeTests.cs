using Moq;
using Shouldly;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Web;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Contexts.ResourceTypes;

/// <summary>
/// Security-focused tests for <see cref="ContentResourceType"/>: live content must only be resolved
/// for an authenticated acting user who has read permission on the node (decision #6 / F-SEC). These
/// cover the gate that short-circuits before the published cache is ever consulted; the resolve/format
/// path itself reuses the well-covered <c>ContentToolHelpers.BuildContentItem</c> / property formatter.
/// </summary>
public class ContentResourceTypeTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessor = new();
    private readonly Mock<IContentPermissionAuthorizer> _authorizer = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessor = new();
    private readonly IUser _user = new Mock<IUser>().Object;

    private ContentResourceType CreateSut()
        => new(new Mock<IAIContextResourceTypeInfrastructure>().Object, _umbracoContextAccessor.Object, _authorizer.Object, _securityAccessor.Object);

    private void WithActingUser(IUser? user)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(s => s.CurrentUser).Returns(user);
        _securityAccessor.SetupGet(a => a.BackOfficeSecurity).Returns(user is null ? null : security.Object);
    }

    [Fact]
    public async Task NoContentId_ReturnsNull_WithoutCheckingPermissionsOrContext()
    {
        var result = await CreateSut().ResolveDataAsync(new ContentResourceSettings { ContentId = null });

        result.ShouldBeNull();
        _authorizer.Verify(a => a.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NoActingUser_ReturnsNull_WithoutPermissionCheck()
    {
        WithActingUser(null);

        var result = await CreateSut().ResolveDataAsync(new ContentResourceSettings { ContentId = Guid.NewGuid() });

        result.ShouldBeNull();
        _authorizer.Verify(a => a.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PermissionDenied_ReturnsNull_WithoutConsultingTheContentCache()
    {
        var key = Guid.NewGuid();
        WithActingUser(_user);
        _authorizer.Setup(a => a.IsDeniedAsync(_user, key, ActionBrowse.ActionLetter)).ReturnsAsync(true);

        var result = await CreateSut().ResolveDataAsync(new ContentResourceSettings { ContentId = key });

        result.ShouldBeNull();
        _umbracoContextAccessor.Verify(a => a.TryGetUmbracoContext(out It.Ref<IUmbracoContext>.IsAny), Times.Never);
    }
}

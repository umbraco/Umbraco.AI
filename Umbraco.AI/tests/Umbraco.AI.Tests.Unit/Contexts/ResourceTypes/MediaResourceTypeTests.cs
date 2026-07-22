using Moq;
using Shouldly;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Web;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Contexts.ResourceTypes;

/// <summary>
/// Security-focused tests for <see cref="MediaResourceType"/>: live media must only be resolved for an
/// authenticated acting user who has read permission on the item (decision #6 / F-SEC). These cover the
/// gate that short-circuits before the published cache is ever consulted; the resolve/format path itself
/// reuses the well-covered <c>ContentToolHelpers.BuildContentItem</c> / property formatter.
/// </summary>
public class MediaResourceTypeTests
{
    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessor = new();
    private readonly Mock<IMediaPermissionAuthorizer> _authorizer = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessor = new();
    private readonly IUser _user = new Mock<IUser>().Object;

    private MediaResourceType CreateSut()
        => new(new Mock<IAIContextResourceTypeInfrastructure>().Object, _umbracoContextAccessor.Object, _authorizer.Object, _securityAccessor.Object);

    private void WithActingUser(IUser? user)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(s => s.CurrentUser).Returns(user);
        _securityAccessor.SetupGet(a => a.BackOfficeSecurity).Returns(user is null ? null : security.Object);
    }

    private static MediaResourceSettings Picked(Guid key) =>
        new() { Media = [new MediaResourcePickedItem { MediaKey = key }] };

    [Fact]
    public async Task NoMediaPicked_ReturnsNull_WithoutCheckingPermissionsOrContext()
    {
        var result = await CreateSut().ResolveDataAsync(new MediaResourceSettings { Media = null });

        result.ShouldBeNull();
        _authorizer.Verify(a => a.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task NoActingUser_ReturnsNull_WithoutPermissionCheck()
    {
        WithActingUser(null);

        var result = await CreateSut().ResolveDataAsync(Picked(Guid.NewGuid()));

        result.ShouldBeNull();
        _authorizer.Verify(a => a.IsDeniedAsync(It.IsAny<IUser>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task PermissionDenied_ReturnsNull_WithoutConsultingTheMediaCache()
    {
        var key = Guid.NewGuid();
        WithActingUser(_user);
        _authorizer.Setup(a => a.IsDeniedAsync(_user, key)).ReturnsAsync(true);

        var result = await CreateSut().ResolveDataAsync(Picked(key));

        result.ShouldBeNull();
        _umbracoContextAccessor.Verify(a => a.TryGetUmbracoContext(out It.Ref<IUmbracoContext>.IsAny), Times.Never);
    }
}

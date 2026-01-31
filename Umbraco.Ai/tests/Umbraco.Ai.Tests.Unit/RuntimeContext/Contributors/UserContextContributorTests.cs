using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.RuntimeContext.Contributors;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

namespace Umbraco.Ai.Tests.Unit.RuntimeContext.Contributors;

public class UserContextContributorTests
{
    private readonly Mock<IBackOfficeSecurityAccessor> _securityAccessorMock;
    private readonly Mock<IBackOfficeSecurity> _backOfficeSecurityMock;
    private readonly Mock<IUser> _userMock;
    private readonly UserContextContributor _contributor;

    public UserContextContributorTests()
    {
        _securityAccessorMock = new Mock<IBackOfficeSecurityAccessor>();
        _backOfficeSecurityMock = new Mock<IBackOfficeSecurity>();
        _userMock = new Mock<IUser>();

        _securityAccessorMock
            .Setup(x => x.BackOfficeSecurity)
            .Returns(_backOfficeSecurityMock.Object);

        _contributor = new UserContextContributor(_securityAccessorMock.Object);
    }

    [Fact]
    public void Contribute_WhenUserAuthenticated_AddsSystemMessagePart()
    {
        // Arrange
        SetupAuthenticatedUser();
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts.Count.ShouldBe(1);
        context.SystemMessageParts[0].ShouldContain("## Current User");
    }

    [Fact]
    public void Contribute_WhenUserAuthenticated_IncludesUserKey()
    {
        // Arrange
        var userKey = Guid.NewGuid();
        SetupAuthenticatedUser(key: userKey);
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldContain($"- Key: {userKey}");
    }

    [Fact]
    public void Contribute_WhenUserAuthenticated_IncludesUserName()
    {
        // Arrange
        SetupAuthenticatedUser(name: "John Smith");
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldContain("- Name: John Smith");
    }

    [Fact]
    public void Contribute_WhenUserAuthenticated_IncludesUsername()
    {
        // Arrange
        SetupAuthenticatedUser(username: "john.smith@example.com");
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldContain("- Username: john.smith@example.com");
    }

    [Fact]
    public void Contribute_WhenUserHasLanguage_IncludesLanguage()
    {
        // Arrange
        SetupAuthenticatedUser(language: "en-US");
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldContain("- Language: en-US");
    }

    [Fact]
    public void Contribute_WhenUserHasNoLanguage_OmitsLanguage()
    {
        // Arrange
        SetupAuthenticatedUser(language: null);
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldNotContain("- Language:");
    }

    [Fact]
    public void Contribute_WhenUserHasGroups_IncludesGroups()
    {
        // Arrange
        var groups = new[] { "Administrators", "Editors" };
        SetupAuthenticatedUser(groupNames: groups);
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldContain("- Groups: Administrators, Editors");
    }

    [Fact]
    public void Contribute_WhenUserHasNoGroups_OmitsGroups()
    {
        // Arrange
        SetupAuthenticatedUser(groupNames: []);
        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts[0].ShouldNotContain("- Groups:");
    }

    [Fact]
    public void Contribute_WhenNoUser_DoesNotModifyContext()
    {
        // Arrange
        _backOfficeSecurityMock
            .Setup(x => x.CurrentUser)
            .Returns((IUser?)null);

        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Contribute_WhenNoBackOfficeSecurity_DoesNotModifyContext()
    {
        // Arrange
        _securityAccessorMock
            .Setup(x => x.BackOfficeSecurity)
            .Returns((IBackOfficeSecurity?)null);

        var context = new AiRuntimeContext([]);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.SystemMessageParts.Count.ShouldBe(0);
    }

    [Fact]
    public void Contribute_DoesNotMarkAnyRequestContextItemsAsHandled()
    {
        // Arrange
        SetupAuthenticatedUser();
        var items = new[]
        {
            new AiRequestContextItem { Description = "Test Item", Value = "test" }
        };
        var context = new AiRuntimeContext(items);

        // Act
        _contributor.Contribute(context);

        // Assert
        context.HandledRequestContextItemCount.ShouldBe(0);
    }

    private void SetupAuthenticatedUser(
        Guid? key = null,
        string? name = "Test User",
        string? username = "test@example.com",
        string? language = "en-US",
        string[]? groupNames = null)
    {
        _userMock.Setup(x => x.Key).Returns(key ?? Guid.NewGuid());
        _userMock.Setup(x => x.Name).Returns(name);
        _userMock.Setup(x => x.Username).Returns(username);
        _userMock.Setup(x => x.Language).Returns(language);

        var groups = new List<IReadOnlyUserGroup>();
        foreach (var groupName in groupNames ?? ["Users"])
        {
            var groupMock = new Mock<IReadOnlyUserGroup>();
            groupMock.Setup(x => x.Name).Returns(groupName);
            groups.Add(groupMock.Object);
        }
        _userMock.Setup(x => x.Groups).Returns(groups);

        _backOfficeSecurityMock
            .Setup(x => x.CurrentUser)
            .Returns(_userMock.Object);
    }
}

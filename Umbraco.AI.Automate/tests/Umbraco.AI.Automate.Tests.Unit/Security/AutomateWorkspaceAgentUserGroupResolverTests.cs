// GP-14 (AC1, AC3, AC4)

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Automate.Security;
using Umbraco.Automate.Core.Security;
using Umbraco.Automate.Core.Workspaces;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

using Xunit;

using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Automate.Tests.Unit.Security;

/// <summary>
/// The automate-workspace resolver lists a workspace's service account groups, but only for a caller
/// who can access that workspace.
/// </summary>
public class AutomateWorkspaceAgentUserGroupResolverTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid CallerGroup = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid AutomationGroup = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly Mock<IWorkspaceService> _workspaces = new();
    private readonly Mock<IWorkspaceServiceAccountResolver> _serviceAccounts = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _security = new();

    private AutomateWorkspaceAgentUserGroupResolver CreateResolver()
        => new(_workspaces.Object, _serviceAccounts.Object, _security.Object);

    #region Happy path

    [Fact]
    public async Task Resolve_CallerCanAccessWorkspace_ReturnsServiceAccountGroups()
    {
        // Given a caller with access to the workspace, whose service account is in Automation
        GivenCaller(CallerGroup);
        GivenAccessibleWorkspaces(WorkspaceId);
        GivenServiceAccount(AutomationGroup);

        // When  the workspace is resolved
        var result = await CreateResolver().ResolveUserGroupsAsync(WorkspaceId.ToString());

        // Then  the service account's groups come back, not the caller's
        result.Success.ShouldBeTrue();
        result.Result.ShouldBe([AutomationGroup]);
    }

    [Fact]
    public async Task Resolve_CallerIsAdminWithoutWorkspaceGroup_ReturnsServiceAccountGroups()
    {
        // Given an admin who is in none of the workspace's groups (admins bypass workspace membership)
        GivenCaller(CmsConstants.Security.AdminGroupKey);
        GivenAccessibleWorkspaces();
        GivenServiceAccount(AutomationGroup);

        // When  the workspace is resolved
        var result = await CreateResolver().ResolveUserGroupsAsync(WorkspaceId.ToString());

        // Then  the service account's groups come back
        result.Success.ShouldBeTrue();
        result.Result.ShouldBe([AutomationGroup]);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task Resolve_CallerCannotAccessWorkspace_IsForbiddenAndServiceAccountNotLooked()
    {
        // Given a caller outside the workspace
        GivenCaller(CallerGroup);
        GivenAccessibleWorkspaces(Guid.NewGuid());
        GivenServiceAccount(AutomationGroup);

        // When  the workspace is resolved
        var result = await CreateResolver().ResolveUserGroupsAsync(WorkspaceId.ToString());

        // Then  the result is forbidden (maps to 403, AC3) and the service account is not looked up
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
        _serviceAccounts.Verify(
            s => s.GetServiceAccountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Resolve_NoBackofficeUser_IsForbidden()
    {
        // Given no current backoffice user
        _security.Setup(s => s.BackOfficeSecurity).Returns((IBackOfficeSecurity?)null);

        // When  the workspace is resolved
        var result = await CreateResolver().ResolveUserGroupsAsync(WorkspaceId.ToString());

        // Then  the result is forbidden
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Resolve_ValueIsNotAWorkspaceId_IsForbidden(string value)
    {
        // Given a caller with access to a workspace
        GivenCaller(CallerGroup);
        GivenAccessibleWorkspaces(WorkspaceId);

        // When  a value that is not a workspace id is resolved
        var result = await CreateResolver().ResolveUserGroupsAsync(value);

        // Then  the result is forbidden: there is no such workspace the caller can access
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    [Fact]
    public async Task Resolve_WorkspaceHasNoServiceAccount_IsNoUser()
    {
        // Given a caller with access to a workspace that has no service account
        GivenCaller(CallerGroup);
        GivenAccessibleWorkspaces(WorkspaceId);
        _serviceAccounts
            .Setup(s => s.GetServiceAccountAsync(WorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IUser?)null);

        // When  the workspace is resolved
        var result = await CreateResolver().ResolveUserGroupsAsync(WorkspaceId.ToString());

        // Then  the result is no-user (maps to 400, AC4)
        result.Success.ShouldBeFalse();
        result.Status.ShouldBe(AIAgentUserGroupResolutionStatus.NoUser);
    }

    #endregion

    [Fact]
    public void Kind_IsAutomateWorkspace()
        => CreateResolver().Kind.ShouldBe("automate-workspace");

    private void GivenCaller(params Guid[] groupKeys)
    {
        var security = new Mock<IBackOfficeSecurity>();
        security.Setup(s => s.CurrentUser).Returns(CreateUser(groupKeys));
        _security.Setup(s => s.BackOfficeSecurity).Returns(security.Object);
    }

    private void GivenAccessibleWorkspaces(params Guid[] workspaceIds)
        => _workspaces
            .Setup(w => w.GetAccessibleWorkspaceIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlySet<Guid>)workspaceIds.ToHashSet());

    private void GivenServiceAccount(params Guid[] groupKeys)
        => _serviceAccounts
            .Setup(s => s.GetServiceAccountAsync(WorkspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(groupKeys));

    private static IUser CreateUser(Guid[] groupKeys)
    {
        var groups = groupKeys.Select(key =>
        {
            var group = new Mock<IReadOnlyUserGroup>();
            group.Setup(g => g.Key).Returns(key);
            group.Setup(g => g.Alias).Returns(key == CmsConstants.Security.AdminGroupKey
                ? CmsConstants.Security.AdminGroupAlias
                : "group-" + key);
            return group.Object;
        }).ToArray();

        var user = new Mock<IUser>();
        user.Setup(u => u.Groups).Returns(groups);
        return user.Object;
    }
}

// GP-1 (AC3), GP-2 (AC1, AC4); rules R1, R9, R10, R14

using Umbraco.AI.Core.Security;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Tests.Unit.Security;

/// <summary>
/// AIPermissionService decides rights from the current user's groups and who may use an agent or prompt.
/// </summary>
public class AIPermissionServiceTests
{
    private static readonly string[] AllVerbs =
    [
        AIPermissions.ManageConnections,
        AIPermissions.ManageProfiles,
        AIPermissions.ManageContexts,
        AIPermissions.ManageKnowledgeSets,
        AIPermissions.ManageGuardrails,
        AIPermissions.ManageSettings,
        AIPermissions.ViewAuditLogs,
        AIPermissions.ViewAnalytics,
        AIPermissions.ManageTests,
    ];

    private static IReadOnlyUserGroup Group(Guid key, params string[] permissions)
    {
        var group = new Mock<IReadOnlyUserGroup>();
        group.SetupGet(g => g.Key).Returns(key);
        group.SetupGet(g => g.Permissions).Returns(new HashSet<string>(permissions));
        return group.Object;
    }

    private static AIPermissionService ServiceFor(params IReadOnlyUserGroup[] groups)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Groups).Returns(groups);

        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(s => s.CurrentUser).Returns(user.Object);

        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.SetupGet(a => a.BackOfficeSecurity).Returns(security.Object);

        return new AIPermissionService(accessor.Object);
    }

    #region Happy path

    [Fact]
    public void HasPermission_WhenAnyGroupHoldsTheVerb_ReturnsTrue()
    {
        // Given a user in two groups, one holding Uai.Context.Manage
        var service = ServiceFor(
            Group(Guid.NewGuid()),
            Group(Guid.NewGuid(), AIPermissions.ManageContexts));

        // When HasPermission(Uai.Context.Manage) is called
        var result = service.HasPermission(AIPermissions.ManageContexts);

        // Then it returns true
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasPermission_ReadsSavedVerb_FromReadOnlyUserGroupPermissions()
    {
        // Given a group saved with the verb string stored on the user group screen
        var service = ServiceFor(Group(Guid.NewGuid(), "Uai.Context.Manage"));

        // When the group is read through IReadOnlyUserGroup.Permissions
        // Then the constant matches the saved verb, and a different verb does not
        service.HasPermission(AIPermissions.ManageContexts).ShouldBeTrue();
        service.HasPermission(AIPermissions.ManageProfiles).ShouldBeFalse();
    }

    [Fact]
    public void Verbs_AreTheNineSavedStrings()
    {
        AllVerbs.ShouldBe(
        [
            "Uai.Connection.Manage",
            "Uai.Profile.Manage",
            "Uai.Context.Manage",
            "Uai.KnowledgeSet.Manage",
            "Uai.Guardrail.Manage",
            "Uai.Settings.Manage",
            "Uai.AuditLog.View",
            "Uai.Analytics.View",
            "Uai.Test.Manage",
        ]);
    }

    [Fact]
    public void CanUse_WhenListIsEmpty_ReturnsTrue()
    {
        // Given an empty allowed group list
        var service = ServiceFor();

        // When CanUse is called for any user
        var result = service.CanUse([], [Guid.NewGuid()]);

        // Then it returns true (R9)
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanUse_WhenListIsNull_ReturnsTrue()
    {
        // Given a null allowed group list
        var service = ServiceFor();

        // When CanUse is called for any user
        var result = service.CanUse(null, [Guid.NewGuid()]);

        // Then it returns true (R9)
        result.ShouldBeTrue();
    }

    [Fact]
    public void CanUse_WhenUserSharesAGroup_ReturnsTrue()
    {
        // Given a list of [editors] and a user in editors
        var editors = Guid.NewGuid();
        var service = ServiceFor();

        // When CanUse is called
        var result = service.CanUse([editors], [Guid.NewGuid(), editors]);

        // Then it returns true (R10)
        result.ShouldBeTrue();
    }

    [Fact]
    public void GetCurrentUserGroups_ReturnsKeysAndAdminFlag()
    {
        // Given a user in the admin group and one other group
        var other = Guid.NewGuid();
        var service = ServiceFor(Group(CmsConstants.Security.AdminGroupKey), Group(other));

        // When the current user's groups are read
        var result = service.GetCurrentUserGroups();

        // Then both keys come back and IsAdmin is true
        result.GroupKeys.ShouldBe([CmsConstants.Security.AdminGroupKey, other]);
        result.IsAdmin.ShouldBeTrue();
    }

    #endregion

    #region Sad path

    [Fact]
    public void HasPermission_WhenNoGroupHoldsTheVerb_ReturnsFalse()
    {
        // Given a user whose groups hold every AI verb except Uai.Connection.Manage
        var service = ServiceFor(
            Group(Guid.NewGuid(), AllVerbs.Where(v => v != AIPermissions.ManageConnections).ToArray()));

        // When HasPermission(Uai.Connection.Manage) is called
        var result = service.HasPermission(AIPermissions.ManageConnections);

        // Then it returns false
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPermission_ForAdminGroupWithoutTheVerb_ReturnsFalse()
    {
        // Given a user in the admin group, which lacks Uai.Connection.Manage
        var service = ServiceFor(Group(CmsConstants.Security.AdminGroupKey));

        // When HasPermission(Uai.Connection.Manage) is called
        var result = service.HasPermission(AIPermissions.ManageConnections);

        // Then it returns false (no admin bypass, R1)
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasPermission_WhenThereIsNoCurrentUser_ReturnsFalse()
    {
        // Given no current backoffice user
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.SetupGet(a => a.BackOfficeSecurity).Returns((IBackOfficeSecurity?)null);
        var service = new AIPermissionService(accessor.Object);

        // When HasPermission is called
        // Then it returns false, and the user has no groups
        service.HasPermission(AIPermissions.ManageConnections).ShouldBeFalse();
        service.GetCurrentUserGroups().GroupKeys.ShouldBeEmpty();
    }

    [Fact]
    public void CanUse_WhenUserSharesNoGroup_ReturnsFalse()
    {
        // Given a list of [editors] and a user in translators only
        var service = ServiceFor();

        // When CanUse is called
        var result = service.CanUse([Guid.NewGuid()], [Guid.NewGuid()]);

        // Then it returns false (R10)
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanUse_WhenOnlyGroupWasDeleted_ReturnsFalse()
    {
        // Given a list holding only the key of a deleted group (no live user has that key)
        var deletedGroup = Guid.NewGuid();
        var service = ServiceFor();

        // When CanUse is called for any user
        var result = service.CanUse([deletedGroup], [Guid.NewGuid(), CmsConstants.Security.AdminGroupKey]);

        // Then it returns false (R14)
        result.ShouldBeFalse();
    }

    #endregion

    #region Visibility of limited items

    private const string AddOnManageVerb = "Uai.AddOn.Manage";

    [Fact]
    public void GetListFilter_ForManagerAskingForRestricted_ReturnsNull()
    {
        // Given a user holding the add-on's manage verb
        var service = ServiceFor(Group(Guid.NewGuid(), AddOnManageVerb));

        // When the list filter is read with managed access asked for
        var result = service.GetListFilter(AddOnManageVerb, includeRestricted: true);

        // Then no group filter applies
        result.ShouldBeNull();
    }

    [Fact]
    public void GetListFilter_ForManagerNotAskingForRestricted_ReturnsTheirGroups()
    {
        // Given a user holding the add-on's manage verb
        var editors = Guid.NewGuid();
        var service = ServiceFor(Group(editors, AddOnManageVerb));

        // When the list filter is read without asking for managed access
        var result = service.GetListFilter(AddOnManageVerb, includeRestricted: false);

        // Then the manage verb does not lift the limit
        result.ShouldBe([editors]);
    }

    [Fact]
    public void GetListFilter_ForNonManagerAskingForRestricted_ReturnsTheirGroups()
    {
        // Given a user without the add-on's manage verb
        var editors = Guid.NewGuid();
        var service = ServiceFor(Group(editors, AIPermissions.ManageConnections));

        // When the list filter is read with managed access asked for
        var result = service.GetListFilter(AddOnManageVerb, includeRestricted: true);

        // Then the limit still applies
        result.ShouldBe([editors]);
    }

    [Fact]
    public void GetListFilter_WithNoCurrentUser_ReturnsNoGroupsEvenWhenAskingForRestricted()
    {
        // Given no current backoffice user
        var accessor = new Mock<IBackOfficeSecurityAccessor>();
        accessor.SetupGet(a => a.BackOfficeSecurity).Returns((IBackOfficeSecurity?)null);
        var service = new AIPermissionService(accessor.Object);

        // When the list filter is read with managed access asked for
        var result = service.GetListFilter(AddOnManageVerb, includeRestricted: true);

        // Then a headless caller gets an empty group list, not null, so limited items stay hidden
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public void IsVisible_ForMemberOfTheItemsGroup_ReturnsTrue()
    {
        var editors = Guid.NewGuid();
        var service = ServiceFor(Group(editors));

        service.IsVisible([editors], AddOnManageVerb, includeRestricted: false).ShouldBeTrue();
    }

    [Fact]
    public void IsVisible_ForOutsiderWithoutManagedAccess_ReturnsFalse()
    {
        var service = ServiceFor(Group(Guid.NewGuid(), AddOnManageVerb));

        // Holding the manage verb does not help unless managed access is asked for
        service.IsVisible([Guid.NewGuid()], AddOnManageVerb, includeRestricted: false).ShouldBeFalse();
    }

    [Fact]
    public void IsVisible_ForManagerAskingForRestricted_ReturnsTrueEvenOutsideTheGroups()
    {
        var service = ServiceFor(Group(Guid.NewGuid(), AddOnManageVerb));

        service.IsVisible([Guid.NewGuid()], AddOnManageVerb, includeRestricted: true).ShouldBeTrue();
    }

    [Fact]
    public void IsVisible_ForNonManagerAskingForRestricted_ReturnsFalseOutsideTheGroups()
    {
        var service = ServiceFor(Group(Guid.NewGuid()));

        service.IsVisible([Guid.NewGuid()], AddOnManageVerb, includeRestricted: true).ShouldBeFalse();
    }

    [Fact]
    public void IsVisible_ForItemWithNoLimit_ReturnsTrueForAnyone()
    {
        var service = ServiceFor(Group(Guid.NewGuid()));

        service.IsVisible(null, AddOnManageVerb, includeRestricted: false).ShouldBeTrue();
    }

    [Fact]
    public void CanCurrentUserUse_ForManagerOutsideTheGroups_ReturnsFalse()
    {
        // Given a manager outside the item's groups
        var service = ServiceFor(Group(Guid.NewGuid(), AddOnManageVerb));

        // When availability is checked
        // Then managing does not lift the limit when using the item
        service.CanCurrentUserUse([Guid.NewGuid()]).ShouldBeFalse();
    }

    [Fact]
    public void CanCurrentUserUse_ForMember_ReturnsTrue()
    {
        var editors = Guid.NewGuid();
        var service = ServiceFor(Group(editors));

        service.CanCurrentUserUse([editors]).ShouldBeTrue();
    }

    #endregion
}

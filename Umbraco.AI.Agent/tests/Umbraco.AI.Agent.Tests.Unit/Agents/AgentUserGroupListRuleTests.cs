// GP-8 (AC1, AC2, AC4, AC6, AC7); spec guess 5

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

using Xunit;

using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Non-admins may only change an agent's group list within their own groups, on agents they can use.
/// </summary>
public class AgentUserGroupListRuleTests
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();

    private readonly Mock<IAIAgentRepository> _repositoryMock = new();
    private readonly Mock<IAIEntityVersionService> _versionServiceMock = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _accessorMock = new();

    public AgentUserGroupListRuleTests()
    {
        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAgent a, Guid? _, CancellationToken _) => a);
    }

    #region Happy path

    [Fact]
    public async Task Save_ByAdmin_AnyGroups_Succeeds()
    {
        // Given an admin, and an agent limited to translators
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Translators]);
        GivenUser(CmsConstants.Security.AdminGroupKey);

        // When  they set the agent's groups to editors and save
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, [Editors]));

        // Then  the save succeeds (AC1)
        result.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task Save_ByAdmin_EmptyingTheList_Succeeds()
    {
        // Given an admin, and an agent limited to editors
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Editors]);
        GivenUser(CmsConstants.Security.AdminGroupKey);

        // When  they empty the list and save
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, []));

        // Then  the save succeeds
        result.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Save_ByMemberAddingOwnGroup_Succeeds()
    {
        // Given a non-admin with Uai.Agent.Manage in editors and an unlimited agent
        var id = Guid.NewGuid();
        GivenStoredAgent(id, []);
        GivenUser(Editors);

        // When  they add editors and save
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, [Editors]));

        // Then  the save succeeds (AC2)
        result.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task Save_ByNonAdmin_LeavingTheListAlone_OnAgentTheyCannotUse_Succeeds()
    {
        // Given an agent limited to translators and a non-admin manager not in it
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Translators]);
        GivenUser(Editors);

        // When  they save it with the same list and a new name (R12 only guards the list)
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, [Translators], "Renamed"));

        // Then  the save succeeds
        result.Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task Save_WithNoCurrentUser_ChangingTheList_Succeeds()
    {
        // Given a headless save (Deploy import) of an agent limited to translators, with no current user
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Translators]);

        // When  it changes the list to editors
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, [Editors]));

        // Then  the save succeeds, as the CMS skips its check without a performing user
        result.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task Save_AddingSomeoneElsesGroup_IsRefused()
    {
        // Given a non-admin in editors only, and an unlimited agent
        var id = Guid.NewGuid();
        GivenStoredAgent(id, []);
        GivenUser(Editors);

        // When  they save the agent adding translators
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, [Translators]));

        // Then  the save is refused (AC4)
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_RemovingSomeoneElsesGroup_IsRefused()
    {
        // Given an agent limited to editors and translators, and a non-admin in editors only
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Editors, Translators]);
        GivenUser(Editors);

        // When  they remove translators and save
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, [Editors]));

        // Then  the save is refused
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_OnAgentTheyCannotUse_IsRefused()
    {
        // Given an agent limited to translators and a non-admin manager not in it
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Translators]);
        GivenUser(Editors);

        // When  they change its group list, adding only their own group
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, [Translators, Editors]));

        // Then  the save is refused (R12, backs AC5)
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_EmptyingTheList_ByNonAdmin_IsRefused()
    {
        // Given an agent limited to editors and a non-admin manager in editors
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Editors]);
        GivenUser(Editors);

        // When  they remove editors and save
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, []));

        // Then  the save is refused (AC6)
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task RefusedSave_KeepsOtherChangesOut()
    {
        // Given a non-admin in editors only
        var id = Guid.NewGuid();
        GivenStoredAgent(id, []);
        GivenUser(Editors);

        // When  they save adding translators and renaming the agent
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, [Translators], "Renamed"));
        await Should.ThrowAsync<AIPermissionDeniedException>(save);

        // Then  nothing is saved, not even a version snapshot (AC7)
        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _versionServiceMock.Verify(
            x => x.SaveVersionAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Save_NewAgent_WithSomeoneElsesGroup_IsRefused()
    {
        // Given a non-admin in editors only, and an agent with no stored row
        var id = Guid.NewGuid();
        GivenUser(Editors);

        // When  they create it limited to translators
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, [Translators]));

        // Then  the save is refused, as a new agent is checked against an empty list
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Rollback_ChangingGroupList_ByNonAdmin_IsRefused()
    {
        // Given an agent saved with no groups, then limited to editors by an admin, and a non-admin manager in editors
        var id = Guid.NewGuid();
        GivenStoredAgent(id, [Editors]);
        _versionServiceMock
            .Setup(x => x.GetVersionSnapshotAsync<AIAgent>(id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAgent(id, []));
        GivenUser(Editors);
        var adapter = new AIAgentVersionableEntityAdapter(CreateService(), _versionServiceMock.Object);

        // When  they roll it back to the first version
        var rollback = () => adapter.RollbackAsync(id, 1);

        // Then  the rollback is refused (AC8, R23), and nothing is restored
        await Should.ThrowAsync<AIPermissionDeniedException>(rollback);
        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    private void GivenStoredAgent(Guid id, Guid[] groups)
        => _repositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAgent(id, groups));

    private void GivenUser(params Guid[] groupKeys)
    {
        var groups = groupKeys.Select(key =>
        {
            var group = new Mock<IReadOnlyUserGroup>();
            group.SetupGet(g => g.Key).Returns(key);
            group.SetupGet(g => g.Permissions).Returns(new HashSet<string>());
            return group.Object;
        }).ToArray();

        var user = new Mock<IUser>();
        user.SetupGet(u => u.Key).Returns(Guid.NewGuid());
        user.SetupGet(u => u.Groups).Returns(groups);

        var security = new Mock<IBackOfficeSecurity>();
        security.SetupGet(s => s.CurrentUser).Returns(user.Object);
        _accessorMock.SetupGet(a => a.BackOfficeSecurity).Returns(security.Object);
    }

    private AIAgentService CreateService() => new(
        _repositoryMock.Object,
        _versionServiceMock.Object,
        null!, // IAIAgentFactory
        null!, // IAGUIStreamingService
        null!, // IAGUIContextConverter
        null!, // IAGUIMessageConverter
        new AIToolCollection(() => []),
        null!, // IAIProfileService
        null!, // IAIGuardrailService
        null!, // IAIContextService
        Mock.Of<IEventAggregator>(),
        new AIPermissionService(_accessorMock.Object),
        _accessorMock.Object);

    private static AIAgent CreateAgent(Guid id, Guid[] groups, string name = "Agent")
        => new(id) { Alias = "agent", Name = name, AllowedUserGroupIds = groups };
}

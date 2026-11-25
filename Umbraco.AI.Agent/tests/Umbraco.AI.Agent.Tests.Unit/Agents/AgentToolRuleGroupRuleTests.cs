// GP-18 (AC1, AC2, AC5-AC8); SPEC R22, R23

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
/// Non-admins may only change an agent's tool rules for groups they belong to.
/// </summary>
public class AgentToolRuleGroupRuleTests
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();

    private readonly Mock<IAIAgentRepository> _repositoryMock = new();
    private readonly Mock<IAIEntityVersionService> _versionServiceMock = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _accessorMock = new();

    public AgentToolRuleGroupRuleTests()
    {
        _repositoryMock
            .Setup(x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIAgent a, Guid? _, CancellationToken _) => a);
    }

    #region Happy path

    [Fact]
    public async Task Save_ByAdmin_ChangingAnyGroupsRules_Succeeds()
    {
        // Given an admin and an agent with tool rules for translators
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));
        GivenUser(CmsConstants.Security.AdminGroupKey);

        // When  they change the translators rules and save
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, Rules((Translators, ["tool-b"]))));

        // Then  the save succeeds (AC1)
        ((AIStandardAgentConfig)result.Config!).UserGroupPermissions[Translators].AllowedToolIds.ShouldBe(["tool-b"]);
    }

    [Fact]
    public async Task Save_ByMember_ChangingOwnGroupsRules_Succeeds()
    {
        // Given a non-admin with Uai.Agent.Manage in editors, and an agent with rules for editors and translators
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Editors, ["tool-a"]), (Translators, ["tool-x"])));
        GivenUser(Editors);

        // When  they change the editors rules and save
        var result = await CreateService().SaveAgentAsync(
            CreateAgent(id, Rules((Editors, ["tool-b"]), (Translators, ["tool-x"]))));

        // Then  the save succeeds (AC2)
        ((AIStandardAgentConfig)result.Config!).UserGroupPermissions[Editors].AllowedToolIds.ShouldBe(["tool-b"]);
    }

    [Fact]
    public async Task Save_ByNonAdmin_ResendingOtherGroupsRulesUnchanged_Succeeds()
    {
        // Given a non-admin in editors and an agent with translators rules
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a", "tool-b"])));
        GivenUser(Editors);

        // When  they save equal rules for translators (a new instance, same tools in another order) with a new name
        var result = await CreateService().SaveAgentAsync(
            CreateAgent(id, Rules((Translators, ["tool-b", "tool-a"])), "Renamed"));

        // Then  the save succeeds, as nothing changed for translators
        result.Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task Save_WithNoCurrentUser_ChangingRules_Succeeds()
    {
        // Given a headless save (Deploy import) of an agent with translators rules
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));

        // When  it changes the translators rules
        var result = await CreateService().SaveAgentAsync(CreateAgent(id, Rules((Translators, ["tool-b"]))));

        // Then  the save succeeds, as the group-list rule does
        ((AIStandardAgentConfig)result.Config!).UserGroupPermissions[Translators].AllowedToolIds.ShouldBe(["tool-b"]);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task Save_ChangingOtherGroupsRules_IsRefused()
    {
        // Given a non-admin in editors only and an agent with tool rules for translators
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));
        GivenUser(Editors);

        // When  they save changed translators rules
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, Rules((Translators, ["tool-b"]))));

        // Then  AIPermissionDeniedException is thrown (AC5)
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_AddingOtherGroupsRules_IsRefused()
    {
        // Given a non-admin in editors only and an agent with no tool rules
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules());
        GivenUser(Editors);

        // When  they save rules for translators
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, Rules((Translators, ["tool-a"]))));

        // Then  AIPermissionDeniedException is thrown (R22)
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_RemovingOtherGroupsRules_IsRefused()
    {
        // Given the same non-admin and an agent with translators rules
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));
        GivenUser(Editors);

        // When  they save without the translators entry
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, Rules()));

        // Then  AIPermissionDeniedException is thrown (AC6)
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_ChangingADenyListOfOtherGroup_IsRefused()
    {
        // Given a non-admin in editors and translators rules with one allowed tool
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));
        GivenUser(Editors);

        // When  they save the same allow list but a new denied tool
        var incoming = Rules((Translators, ["tool-a"]));
        incoming.UserGroupPermissions[Translators].DeniedToolIds = ["tool-z"];
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, incoming));

        // Then  AIPermissionDeniedException is thrown, as every list of the entry counts
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_NewAgent_WithOtherGroupsRules_IsRefused()
    {
        // Given a non-admin in editors only, and an agent with no stored row
        var id = Guid.NewGuid();
        GivenUser(Editors);

        // When  they create it with rules for translators
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, Rules((Translators, ["tool-a"]))));

        // Then  AIPermissionDeniedException is thrown
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task RefusedSave_KeepsOtherChangesOut()
    {
        // Given the same non-admin and agent
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));
        GivenUser(Editors);

        // When  they save changed translators rules and a new agent name
        var save = () => CreateService().SaveAgentAsync(CreateAgent(id, Rules((Translators, ["tool-b"])), "Renamed"));
        await Should.ThrowAsync<AIPermissionDeniedException>(save);

        // Then  nothing is saved, so the agent keeps its old name (AC7)
        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _versionServiceMock.Verify(
            x => x.SaveVersionAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Rollback_ChangingOtherGroupsRules_ByNonAdmin_IsRefused()
    {
        // Given translators rules changed by an admin since an older version, and a non-admin not in translators
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-new"])));
        _versionServiceMock
            .Setup(x => x.GetVersionSnapshotAsync<AIAgent>(id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAgent(id, Rules((Translators, ["tool-old"]))));
        GivenUser(Editors);
        var adapter = new AIAgentVersionableEntityAdapter(CreateService(), _versionServiceMock.Object);

        // When  they roll the agent back to that version
        var rollback = () => adapter.RollbackAsync(id, 1);

        // Then  AIPermissionDeniedException is thrown (AC8), and nothing is restored
        await Should.ThrowAsync<AIPermissionDeniedException>(rollback);
        _repositoryMock.Verify(
            x => x.SaveAsync(It.IsAny<AIAgent>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    [Fact]
    public async Task SaveUnrestricted_ByNonAdmin_ChangingAnotherGroupsRules_Succeeds()
    {
        // Given a non-admin in editors and an agent with translators rules
        var id = Guid.NewGuid();
        GivenStoredAgent(id, Rules((Translators, ["tool-a"])));
        GivenUser(Editors);

        // When  an import started by them saves different translators rules
        var result = await CreateService().SaveAgentUnrestrictedAsync(CreateAgent(id, Rules((Translators, ["tool-b"]))));

        // Then  the exported rules are saved, as the rule does not apply to an import
        ((AIStandardAgentConfig)result.Config!).UserGroupPermissions[Translators].AllowedToolIds.ShouldBe(["tool-b"]);
    }

    private void GivenStoredAgent(Guid id, AIStandardAgentConfig config)
        => _repositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAgent(id, config));

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

    private static AIStandardAgentConfig Rules(params (Guid Group, string[] AllowedToolIds)[] rules)
        => new()
        {
            UserGroupPermissions = rules.ToDictionary(
                r => r.Group,
                r => new AIAgentUserGroupPermissions { AllowedToolIds = r.AllowedToolIds }),
        };

    private static AIAgent CreateAgent(Guid id, AIStandardAgentConfig config, string name = "Agent")
        => new(id) { Alias = "agent", Name = name, Config = config };
}

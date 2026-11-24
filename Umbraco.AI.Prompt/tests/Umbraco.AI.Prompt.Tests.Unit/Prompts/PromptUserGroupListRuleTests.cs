// GP-12 (AC4); the group-list save rule (T17) applied to prompts

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;

using Xunit;

using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

/// <summary>
/// Non-admins may only change a prompt's group list within their own groups, on prompts they can use.
/// </summary>
public class PromptUserGroupListRuleTests
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();

    private readonly InMemoryAIPromptRepository _repository = new();
    private readonly Mock<IAIEntityVersionService> _versionServiceMock = new();
    private readonly Mock<IBackOfficeSecurityAccessor> _accessorMock = new();
    private readonly Mock<IEventAggregator> _eventAggregatorMock = new();

    #region Happy path

    [Fact]
    public async Task Save_ByAdmin_AnyGroups_Succeeds()
    {
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Translators]);
        GivenUser(CmsConstants.Security.AdminGroupKey);

        var result = await CreateService().SavePromptAsync(CreatePrompt(id, [Editors]));

        result.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task Save_ByAdmin_EmptyingTheList_Succeeds()
    {
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Editors]);
        GivenUser(CmsConstants.Security.AdminGroupKey);

        var result = await CreateService().SavePromptAsync(CreatePrompt(id, []));

        result.AllowedUserGroupIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task Save_ByMemberAddingOwnGroup_Succeeds()
    {
        // Given a non-admin in editors and an unlimited prompt
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, []);
        GivenUser(Editors);

        // When  they add editors and save
        var result = await CreateService().SavePromptAsync(CreatePrompt(id, [Editors]));

        // Then  the save succeeds
        result.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task Save_ByNonAdmin_LeavingTheListAlone_OnPromptTheyCannotUse_Succeeds()
    {
        // Given a prompt limited to translators and a non-admin not in it
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Translators]);
        GivenUser(Editors);

        // When  they save it with the same list and a new name (the rule only guards the list)
        var result = await CreateService().SavePromptAsync(CreatePrompt(id, [Translators], "Renamed"));

        // Then  the save succeeds
        result.Name.ShouldBe("Renamed");
    }

    [Fact]
    public async Task Save_WithNoCurrentUser_ChangingTheList_Succeeds()
    {
        // Given a headless save (Deploy import) of a prompt limited to translators, with no current user
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Translators]);

        // When  it changes the list to editors
        var result = await CreateService().SavePromptAsync(CreatePrompt(id, [Editors]));

        // Then  the save succeeds, as the CMS skips its check without a performing user
        result.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task Save_AddingSomeoneElsesGroup_IsRefused()
    {
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, []);
        GivenUser(Editors);

        var save = () => CreateService().SavePromptAsync(CreatePrompt(id, [Translators]));

        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_RemovingSomeoneElsesGroup_IsRefused()
    {
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Editors, Translators]);
        GivenUser(Editors);

        var save = () => CreateService().SavePromptAsync(CreatePrompt(id, [Editors]));

        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_OnPromptTheyCannotUse_IsRefused()
    {
        // Given a prompt limited to translators and a non-admin not in it
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Translators]);
        GivenUser(Editors);

        // When  they change its group list, adding only their own group
        var save = () => CreateService().SavePromptAsync(CreatePrompt(id, [Translators, Editors]));

        // Then  the save is refused
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_EmptyingTheList_ByNonAdmin_IsRefused()
    {
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Editors]);
        GivenUser(Editors);

        var save = () => CreateService().SavePromptAsync(CreatePrompt(id, []));

        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task Save_NewPrompt_WithSomeoneElsesGroup_IsRefused()
    {
        // Given a non-admin in editors and a prompt with no stored row
        var id = Guid.NewGuid();
        GivenUser(Editors);

        // When  they create it limited to translators
        var save = () => CreateService().SavePromptAsync(CreatePrompt(id, [Translators]));

        // Then  the save is refused, as a new prompt is checked against an empty list
        await Should.ThrowAsync<AIPermissionDeniedException>(save);
    }

    [Fact]
    public async Task RefusedSave_WritesNothing_AndPublishesNothing()
    {
        // Given a non-admin in editors and an unlimited prompt
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, []);
        GivenUser(Editors);

        // When  they save adding translators and renaming the prompt
        var save = () => CreateService().SavePromptAsync(CreatePrompt(id, [Translators], "Renamed"));
        await Should.ThrowAsync<AIPermissionDeniedException>(save);

        // Then  nothing is saved, no version snapshot is taken, and no notification fired
        (await _repository.GetByIdAsync(id))!.Name.ShouldBe("stored");
        _versionServiceMock.Verify(
            x => x.SaveVersionAsync(It.IsAny<AIPrompt>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _eventAggregatorMock.Verify(
            x => x.PublishAsync(It.IsAny<AIPromptSavingNotification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Rollback_ChangingGroupList_ByNonAdmin_IsRefused()
    {
        // Given a prompt limited to editors, an older version with no limit, and a non-admin in editors
        var id = Guid.NewGuid();
        await GivenStoredPromptAsync(id, [Editors]);
        _versionServiceMock
            .Setup(x => x.GetVersionSnapshotAsync<AIPrompt>(id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePrompt(id, []));
        GivenUser(Editors);
        var adapter = new AIPromptVersionableEntityAdapter(CreateService(), _versionServiceMock.Object);

        // When  they roll it back to the first version
        var rollback = () => adapter.RollbackAsync(id, 1);

        // Then  the rollback is refused, and nothing is restored
        await Should.ThrowAsync<AIPermissionDeniedException>(rollback);
        (await _repository.GetByIdAsync(id))!.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    #endregion

    private async Task GivenStoredPromptAsync(Guid id, Guid[] groups)
        => await _repository.SaveAsync(CreatePrompt(id, groups, "stored"));

    private static AIPrompt CreatePrompt(Guid id, Guid[] groups, string name = "stored") => new(id)
    {
        Alias = "summarize",
        Name = name,
        Instructions = "Summarize",
        AllowedUserGroupIds = groups,
    };

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

    private AIPromptService CreateService() => new(
        _repository,
        _versionServiceMock.Object,
        Mock.Of<IAIChatService>(),
        Mock.Of<IAIPromptTemplateService>(),
        Mock.Of<IServiceScopeFactory>(),
        new AIToolCollection(() => []),
        Mock.Of<IAIFunctionFactory>(),
        Mock.Of<IAIRuntimeContextScopeProvider>(),
        new AIRuntimeContextContributorCollection(() => []),
        _eventAggregatorMock.Object,
        Mock.Of<IAIPromptPropertyValueSchemaResolver>(),
        new AISimplifiedPropertyValueTransformerCollection(() => []),
        Mock.Of<ILogger<AIPromptService>>(),
        new AIPermissionService(_accessorMock.Object),
        _accessorMock.Object);
}

// GP-13 (AC2, AC3, AC5); GP-12 (AC4, AC5)

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

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

/// <summary>
/// IAIPromptService hides and refuses limited prompts for users outside their groups, unless a manager asks
/// for managed access.
/// </summary>
public class PromptUsageLimitTests
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();

    private static readonly AIPromptExecutionRequest Request = new()
    {
        EntityId = Guid.NewGuid(),
        EntityType = "document",
        PropertyAlias = "title",
        ContentTypeAlias = "homePage",
    };

    #region Happy path

    [Fact]
    public async Task List_ForMember_IncludesLimitedPrompt()
    {
        // Given a prompt limited to editors and a user in editors
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When prompts are listed
        var listed = await harness.Service.GetPromptsAsync();
        var (paged, total) = await harness.Service.GetPromptsPagedAsync(0, 10);

        // Then the prompt is included (GP-13 AC2)
        listed.ShouldContain(limited);
        paged.ShouldContain(limited);
        total.ShouldBe(1);
    }

    [Fact]
    public async Task List_ForAnyone_IncludesPromptWithNoLimit()
    {
        var harness = new Harness(inGroups: []);
        var open = harness.Add("open", allowedGroups: []);

        var listed = await harness.Service.GetPromptsAsync();

        listed.ShouldContain(open);
    }

    [Fact]
    public async Task Get_ForMember_ReturnsLimitedPrompt()
    {
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        (await harness.Service.GetPromptAsync(limited.Id)).ShouldBe(limited);
        (await harness.Service.GetPromptByAliasAsync("limited")).ShouldBe(limited);
    }

    [Fact]
    public async Task Get_WithIncludeRestricted_ByManager_ReturnsPromptTheyCannotUse()
    {
        // Given a prompt limited to translators and a user in editors who holds Manage prompts
        var harness = new Harness(inGroups: [Editors], permissions: [AIPermissions.ManagePrompts]);
        var limited = harness.Add("limited", allowedGroups: [Translators]);

        // When they ask for managed access
        var byId = await harness.Service.GetPromptAsync(limited.Id, includeRestricted: true);
        var byAlias = await harness.Service.GetPromptByAliasAsync("limited", includeRestricted: true);
        var (paged, total) = await harness.Service.GetPromptsPagedAsync(0, 10, null, null, includeRestricted: true);

        // Then they see it (GP-12 AC5)
        byId.ShouldBe(limited);
        byAlias.ShouldBe(limited);
        paged.ShouldContain(limited);
        total.ShouldBe(1);
    }

    [Fact]
    public async Task Execute_ForMember_PassesTheAvailabilityCheck()
    {
        // Given a prompt limited to editors and a user in editors
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When the prompt is executed
        var run = () => harness.Service.ExecutePromptAsync(limited.Id, Request, harness.NoScopeValidation);

        // Then it got past the check, as far as the executing notification that cancels it (GP-13 AC5)
        var thrown = await Should.ThrowAsync<InvalidOperationException>(run);
        thrown.ShouldNotBeOfType<AIPromptNotAvailableException>();
        thrown.Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task Unrestricted_ReadsLimitedPromptWhoeverIsSignedIn()
    {
        // Given a prompt limited to translators and a user in editors
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Translators]);

        // When it is read through the unrestricted reader
        var reader = (IAIPromptUnrestrictedReader)harness.Service;
        var byId = await reader.GetPromptUnrestrictedAsync(limited.Id);
        var all = await reader.GetPromptsUnrestrictedAsync();

        // Then it is returned, which is how resolvers, telemetry and Deploy read prompts
        byId.ShouldBe(limited);
        all.ShouldContain(limited);
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task List_ForNonMember_ExcludesLimitedPrompt()
    {
        // Given a prompt limited to editors and a user in translators
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors]);

        // When prompts are listed
        var listed = await harness.Service.GetPromptsAsync();
        var (paged, total) = await harness.Service.GetPromptsPagedAsync(0, 10);

        // Then the prompt isn't listed, and the total doesn't count it (GP-13 AC2)
        listed.ShouldBeEmpty();
        paged.ShouldBeEmpty();
        total.ShouldBe(0);
    }

    [Fact]
    public async Task List_WithIncludeRestricted_ByNonManager_StillExcludesLimitedPrompt()
    {
        // Given a prompt limited to editors, and a user in translators without Manage prompts
        var harness = new Harness(inGroups: [Translators]);
        harness.Add("limited", allowedGroups: [Editors]);

        // When they ask for managed access
        var (paged, total) = await harness.Service.GetPromptsPagedAsync(0, 10, null, null, includeRestricted: true);

        // Then the request is ignored (GP-12 AC5)
        paged.ShouldBeEmpty();
        total.ShouldBe(0);
    }

    [Fact]
    public async Task Get_ForNonMember_ReadsAsNotFound()
    {
        // Given a prompt limited to editors and a user in translators
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When it is fetched by id or alias
        var byId = await harness.Service.GetPromptAsync(limited.Id);
        var byAlias = await harness.Service.GetPromptByAliasAsync("limited");

        // Then it is null, as if it didn't exist (GP-13 AC3)
        byId.ShouldBeNull();
        byAlias.ShouldBeNull();
    }

    [Fact]
    public async Task Get_WithIncludeRestricted_ByNonManager_ReadsAsNotFound()
    {
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        (await harness.Service.GetPromptAsync(limited.Id, includeRestricted: true)).ShouldBeNull();
        (await harness.Service.GetPromptByAliasAsync("limited", includeRestricted: true)).ShouldBeNull();
    }

    [Fact]
    public async Task Get_WithNoCurrentUser_ReadsLimitedPromptAsNotFound()
    {
        // Given a prompt limited to editors and no signed-in user
        var harness = new Harness(inGroups: null);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When it is fetched, even asking for managed access
        (await harness.Service.GetPromptAsync(limited.Id)).ShouldBeNull();
        (await harness.Service.GetPromptAsync(limited.Id, includeRestricted: true)).ShouldBeNull();
    }

    [Fact]
    public async Task Execute_ForNonMember_IsRefusedWithATypedFailure()
    {
        // Given a prompt limited to editors and a user in translators
        var harness = new Harness(inGroups: [Translators]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When the prompt is executed
        var run = () => harness.Service.ExecutePromptAsync(limited.Id, Request);

        // Then it is refused with AIPromptNotAvailableException, before anything else happens (GP-13 AC5)
        var thrown = await Should.ThrowAsync<AIPromptNotAvailableException>(run);
        thrown.PromptId.ShouldBe(limited.Id);
        harness.ExecutingNotificationPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task Execute_ByManagerOutsideTheGroups_IsStillRefused()
    {
        // Given a prompt limited to editors, and a user in translators who holds Manage prompts
        var harness = new Harness(inGroups: [Translators], permissions: [AIPermissions.ManagePrompts]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When the prompt is executed
        var run = () => harness.Service.ExecutePromptAsync(limited.Id, Request);

        // Then managing doesn't lift the limit when running
        await Should.ThrowAsync<AIPromptNotAvailableException>(run);
    }

    [Fact]
    public async Task Execute_WithNoCurrentUser_OnLimitedPrompt_IsRefused()
    {
        var harness = new Harness(inGroups: null);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        var run = () => harness.Service.ExecutePromptAsync(limited.Id, Request);

        await Should.ThrowAsync<AIPromptNotAvailableException>(run);
    }

    [Fact]
    public async Task Execute_OnAMissingPrompt_StillReportsNotFound()
    {
        var harness = new Harness(inGroups: [Editors]);

        var run = () => harness.Service.ExecutePromptAsync(Guid.NewGuid(), Request);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(run);
        thrown.ShouldNotBeOfType<AIPromptNotAvailableException>();
        thrown.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task GetForUse_ForMember_ReturnsTheLimitedPrompt()
    {
        // Given a prompt limited to editors and a user in editors
        var harness = new Harness(inGroups: [Editors]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When it is fetched to run, by id and by alias
        // Then it is returned
        (await harness.Service.GetPromptForUseAsync(limited.Id)).ShouldBe(limited);
        (await harness.Service.GetPromptForUseByAliasAsync("limited")).ShouldBe(limited);
    }

    [Fact]
    public async Task GetForUse_ForNonMember_IsRefusedNotHidden()
    {
        // Given a prompt limited to editors and a user in translators, even one who holds Manage prompts
        var harness = new Harness(inGroups: [Translators], permissions: [AIPermissions.ManagePrompts]);
        var limited = harness.Add("limited", allowedGroups: [Editors]);

        // When it is fetched to run, by id and by alias
        var byId = () => harness.Service.GetPromptForUseAsync(limited.Id);
        var byAlias = () => harness.Service.GetPromptForUseByAliasAsync("limited");

        // Then both are refused, so the caller can tell it from a missing prompt (GP-13 AC4)
        (await Should.ThrowAsync<AIPromptNotAvailableException>(byId)).PromptId.ShouldBe(limited.Id);
        await Should.ThrowAsync<AIPromptNotAvailableException>(byAlias);
    }

    [Fact]
    public async Task GetForUse_OnAMissingPrompt_ReturnsNull()
    {
        // Given no prompt
        var harness = new Harness(inGroups: [Editors]);

        // When it is fetched to run, by id and by alias
        // Then null comes back, not a refusal
        (await harness.Service.GetPromptForUseAsync(Guid.NewGuid())).ShouldBeNull();
        (await harness.Service.GetPromptForUseByAliasAsync("missing")).ShouldBeNull();
    }

    #endregion

    internal sealed class Harness
    {
        private readonly InMemoryAIPromptRepository _repository = new();
        private readonly Mock<IEventAggregator> _eventAggregator = new();

        public Harness(Guid[]? inGroups, string[]? permissions = null)
        {
            var accessor = new Mock<IBackOfficeSecurityAccessor>();
            if (inGroups is not null)
            {
                var groups = inGroups.Select(key =>
                {
                    var group = new Mock<IReadOnlyUserGroup>();
                    group.SetupGet(g => g.Key).Returns(key);
                    group.SetupGet(g => g.Permissions).Returns(new HashSet<string>(permissions ?? []));
                    return group.Object;
                }).ToArray();

                var user = new Mock<IUser>();
                user.SetupGet(u => u.Key).Returns(Guid.NewGuid());
                user.SetupGet(u => u.Groups).Returns(groups);

                var security = new Mock<IBackOfficeSecurity>();
                security.SetupGet(s => s.CurrentUser).Returns(user.Object);
                accessor.SetupGet(a => a.BackOfficeSecurity).Returns(security.Object);
            }

            _eventAggregator
                .Setup(x => x.PublishAsync(It.IsAny<AIPromptExecutingNotification>(), It.IsAny<CancellationToken>()))
                .Callback<AIPromptExecutingNotification, CancellationToken>((notification, _) =>
                {
                    ExecutingNotificationPublished = true;
                    notification.Cancel = true;
                })
                .Returns(Task.CompletedTask);

            Service = new AIPromptService(
                _repository,
                Mock.Of<IAIEntityVersionService>(),
                Mock.Of<IAIChatService>(),
                Mock.Of<IAIPromptTemplateService>(),
                Mock.Of<IServiceScopeFactory>(),
                new AIToolCollection(() => []),
                Mock.Of<IAIFunctionFactory>(),
                Mock.Of<IAIRuntimeContextScopeProvider>(),
                new AIRuntimeContextContributorCollection(() => []),
                _eventAggregator.Object,
                Mock.Of<IAIPromptPropertyValueSchemaResolver>(),
                new AISimplifiedPropertyValueTransformerCollection(() => []),
                Mock.Of<ILogger<AIPromptService>>(),
                new AIPermissionService(accessor.Object),
                accessor.Object);
        }

        public AIPromptService Service { get; }

        public bool ExecutingNotificationPublished { get; private set; }

        public AIPromptExecutionOptions NoScopeValidation { get; } = new() { ValidateScope = false };

        public AIPrompt Add(string alias, Guid[] allowedGroups)
        {
            var prompt = new AIPrompt
            {
                Id = Guid.NewGuid(),
                Alias = alias,
                Name = alias,
                Instructions = "Do it",
                AllowedUserGroupIds = allowedGroups,
            };
            _repository.SaveAsync(prompt).GetAwaiter().GetResult();
            return prompt;
        }
    }
}

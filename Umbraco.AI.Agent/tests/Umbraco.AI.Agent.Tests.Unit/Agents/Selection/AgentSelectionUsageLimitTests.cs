// GP-9 (AC6)
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Agents.Selection;
using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Tools;
using Umbraco.Cms.Core.Events;

using Xunit;

using static Umbraco.AI.Agent.Tests.Unit.Agents.Selection.AgentSelectionTestBuilders;

using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;

namespace Umbraco.AI.Agent.Tests.Unit.Agents.Selection;

/// <summary>
/// Automatic agent selection never picks an agent the user can't use.
/// </summary>
public class AgentSelectionUsageLimitTests
{
    private static readonly Guid EditorsGroup = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");
    private static readonly Guid WritersGroup = Guid.Parse("ffffffff-0000-0000-0000-000000000002");

    private static readonly UmbracoAIAgent EditorsOnlyAgent = CreateLimitedAgent(
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), "editors-only", EditorsGroup);

    private static readonly UmbracoAIAgent OpenAgent =
        CreateAgent(Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), "open-agent");

    private static UmbracoAIAgent CreateLimitedAgent(Guid id, string alias, params Guid[] groups)
    {
        var agent = CreateAgent(id, alias);
        agent.AllowedUserGroupIds = groups;
        return agent;
    }

    /// <summary>
    /// A real <see cref="AIAgentSelectionService"/> over a real <see cref="AIAgentService"/>, so the
    /// service's user-group filtering is in effect. Only the repository is a stand-in: it answers the way
    /// the database query does (no limit, or a group in common, or no filter at all).
    /// </summary>
    private static IAIAgentSelectionService CreateService(
        IReadOnlyList<Guid> userGroupIds,
        IReadOnlyList<UmbracoAIAgent> allAgents,
        params IAIAgentSelector[] selectors)
    {
        var accessor = CreateBackOfficeSecurityAccessor(userGroupIds);

        var repository = new Mock<IAIAgentRepository>();
        repository
            .Setup(x => x.GetBySurfaceAsync(SurfaceId, It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyCollection<Guid>? usable, CancellationToken _) =>
                allAgents.Where(a => usable is null
                    || a.AllowedUserGroupIds.Count == 0
                    || a.AllowedUserGroupIds.Any(usable.Contains)));

        var agentService = new AIAgentService(
            repository.Object,
            null!, // IAIEntityVersionService
            null!, // IAIAgentFactory
            null!, // IAGUIStreamingService
            null!, // IAGUIContextConverter
            null!, // IAGUIMessageConverter
            new AIToolCollection(() => []),
            null!, // IAIProfileService
            null!, // IAIGuardrailService
            null!, // IAIContextService
            Mock.Of<IEventAggregator>(),
            new AIPermissionService(accessor),
            backOfficeSecurityAccessor: null);

        return new AIAgentSelectionService(
            agentService,
            new AIAgentSurfaceCollection(() => [new TestSurface()]),
            new AIAgentScopeValidator(),
            new AIAgentSelectorCollection(() => selectors),
            Mock.Of<IEventAggregator>(),
            NullLogger<AIAgentSelectionService>.Instance,
            accessor);
    }

    #region Sad path

    // AC6 - never picked automatically (R11)
    public class GivenTheBestMatchIsLimitedToAnotherGroup
    {
        private readonly AIAgentSelectionResult? _result;

        public GivenTheBestMatchIsLimitedToAnotherGroup()
        {
            // The classifier names the editors-only agent as the best match.
            var (llmSelector, _) = CreateLLMSelector(EditorsOnlyAgent.Id.ToString());
            var service = CreateService(
                userGroupIds: [WritersGroup],
                allAgents: [EditorsOnlyAgent, OpenAgent],
                llmSelector);

            _result = service.SelectAgentAsync(CreateInput()).GetAwaiter().GetResult();
        }

        [Fact]
        public void IsNotPicked() => _result?.Agent.ShouldNotBe(EditorsOnlyAgent);

        [Fact]
        public void AnotherAgentIsPicked() => _result!.Agent.ShouldBe(OpenAgent);
    }

    // The same request from a member of the limiting group still gets the best match.
    public class GivenTheBestMatchIsLimitedToTheUsersGroup
    {
        private readonly AIAgentSelectionResult? _result;

        public GivenTheBestMatchIsLimitedToTheUsersGroup()
        {
            var (llmSelector, _) = CreateLLMSelector(EditorsOnlyAgent.Id.ToString());
            var service = CreateService(
                userGroupIds: [EditorsGroup],
                allAgents: [EditorsOnlyAgent, OpenAgent],
                llmSelector);

            _result = service.SelectAgentAsync(CreateInput()).GetAwaiter().GetResult();
        }

        [Fact]
        public void IsPicked() => _result!.Agent.ShouldBe(EditorsOnlyAgent);
    }

    // Every agent is limited to a group the user isn't in: nothing is picked.
    public class GivenEveryAgentIsLimitedToAnotherGroup
    {
        private readonly AIAgentSelectionResult? _result;

        public GivenEveryAgentIsLimitedToAnotherGroup()
        {
            var (llmSelector, _) = CreateLLMSelector(EditorsOnlyAgent.Id.ToString());
            var service = CreateService(
                userGroupIds: [WritersGroup],
                allAgents: [EditorsOnlyAgent],
                llmSelector);

            _result = service.SelectAgentAsync(CreateInput()).GetAwaiter().GetResult();
        }

        [Fact]
        public void NoAgentIsPicked() => _result.ShouldBeNull();
    }

    #endregion
}

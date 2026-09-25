// Story DE-4 — Only offer Decision judges when Decision is on (AC1, AC4, AC5, AC6, AC9, AC10)
//
// STAGED SPEC (task T4). Drives the real AllGuardrailEvaluatorsController action through a real
// UmbracoMapper + real GuardrailMapDefinition (like AllProviderControllerExperimentalTests). Uses
// fake evaluators, one marked [AIRequiresCapability(AICapability.Decision)] under the id
// "decision-judge", so this doesn't depend on T2's real evaluator.
//
// Assumed production surface (ARCHITECTURE key decision 9):
//   - new ctor AllGuardrailEvaluatorsController(AIGuardrailEvaluatorCollection, IUmbracoMapper,
//     IAIExperimentalFeatures), [ActivatorUtilitiesConstructor];
//   - old ctor (AIGuardrailEvaluatorCollection, IUmbracoMapper) kept [Obsolete], resolving
//     IAIExperimentalFeatures via StaticServiceProvider.Instance.GetRequiredService<T>().
// AC9 sets StaticServiceProvider.Instance (public setter) to a mock provider for the duration of
// the scenario and restores it after; its collection runs with parallelization disabled.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Guardrail.Controllers;
using Umbraco.AI.Web.Api.Management.Guardrail.Mapping;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Guardrail;

[CollectionDefinition(nameof(GuardrailEvaluatorsStaticServiceProviderCollection), DisableParallelization = true)]
public class GuardrailEvaluatorsStaticServiceProviderCollection;

public class AllGuardrailEvaluatorsControllerTests
{
    private const string DecisionJudgeId = "decision-judge";

    private class FakeEvaluator(string id) : IAIGuardrailEvaluator
    {
        public string Id { get; } = id;

        public string Name => Id;

        public string Description => Id;

        public AIGuardrailEvaluatorType Type => AIGuardrailEvaluatorType.CodeBased;

        public Type? ConfigType => null;

        public AIEditableModelSchema? GetConfigSchema() => null;

        public Task<AIGuardrailResult> EvaluateAsync(
            string content,
            IReadOnlyList<ChatMessage> conversationHistory,
            AIGuardrailConfig config,
            CancellationToken cancellationToken)
            => Task.FromResult(new AIGuardrailResult { EvaluatorId = Id, Flagged = false });
    }

    [AIRequiresCapability(AICapability.Decision)]
    private sealed class FakeDecisionEvaluator() : FakeEvaluator(DecisionJudgeId);

    private static AIGuardrailEvaluatorCollection Evaluators()
        => new(() => [new FakeEvaluator("contains"), new FakeEvaluator("regex"), new FakeDecisionEvaluator()]);

    private static IUmbracoMapper Mapper()
        => new UmbracoMapper(
            new MapDefinitionCollection(() => [new GuardrailMapDefinition()]),
            Mock.Of<Umbraco.Cms.Core.Scoping.ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);

    private static IAIExperimentalFeatures Flag(bool decisionOn)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(It.IsAny<AICapability>()))
            .Returns((AICapability c) => c != AICapability.Decision || decisionOn);
        return experimental.Object;
    }

    private static IReadOnlyList<string> ListIds(AllGuardrailEvaluatorsController controller)
        => ((IEnumerable<GuardrailEvaluatorInfoModel>)((OkObjectResult)controller.GetAllGuardrailEvaluators().Result!).Value!)
            .Select(e => e.Id)
            .ToList();

    private static IReadOnlyList<string> ListIds(bool decisionOn)
        => ListIds(new AllGuardrailEvaluatorsController(Evaluators(), Mapper(), Flag(decisionOn)));

    #region Happy path

    public class GivenTheFlagOn
    {
        [Fact(Skip = "Pending T4")]
        public void ListsTheDecisionJudge() => ListIds(decisionOn: true).ShouldContain(DecisionJudgeId);
    }

    public class GivenTheFlagOffAndOtherEvaluators
    {
        [Fact(Skip = "Pending T4")]
        public void StillListsEveryOtherEvaluator()
            => new[] { "contains", "regex" }.ShouldBeSubsetOf(ListIds(decisionOn: false));
    }

    public class GivenTheFlagFlippedOnAtRuntime
    {
        [Fact(Skip = "Pending T4")]
        public void ListsTheDecisionJudgeOnTheNextRequest()
        {
            var decisionOn = false;
            var experimental = new Mock<IAIExperimentalFeatures>();
            experimental.Setup(x => x.IsCapabilityEnabled(It.IsAny<AICapability>()))
                .Returns((AICapability c) => c != AICapability.Decision || decisionOn);
            var controller = new AllGuardrailEvaluatorsController(Evaluators(), Mapper(), experimental.Object);
            ListIds(controller);

            decisionOn = true;

            ListIds(controller).ShouldContain(DecisionJudgeId);
        }
    }

    public class GivenTheFlagOffAndASavedDecisionJudgeRule
    {
        [Fact(Skip = "Pending T4")]
        public void TheCollectionStillResolvesTheEvaluatorById()
        {
            var evaluators = Evaluators();
            ListIds(new AllGuardrailEvaluatorsController(evaluators, Mapper(), Flag(decisionOn: false)));

            evaluators.GetById(DecisionJudgeId).ShouldNotBeNull();
        }
    }

    #endregion

    #region Sad path

    public class GivenTheFlagOff
    {
        [Fact(Skip = "Pending T4")]
        public void OmitsTheDecisionJudge() => ListIds(decisionOn: false).ShouldNotContain(DecisionJudgeId);
    }

    [Collection(nameof(GuardrailEvaluatorsStaticServiceProviderCollection))]
    public class GivenAControllerBuiltThroughItsObsoleteConstructor : IDisposable
    {
        private readonly IServiceProvider? _previous = StaticServiceProvider.Instance;

        public GivenAControllerBuiltThroughItsObsoleteConstructor()
        {
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IAIExperimentalFeatures))).Returns(Flag(decisionOn: false));
            StaticServiceProvider.Instance = services.Object;
        }

        public void Dispose() => StaticServiceProvider.Instance = _previous!;

        [Fact(Skip = "Pending T4")]
        public void FiltersTheSameWay()
        {
#pragma warning disable CS0618 // Exercises the obsolete constructor on purpose
            var controller = new AllGuardrailEvaluatorsController(Evaluators(), Mapper());
#pragma warning restore CS0618

            ListIds(controller).ShouldNotContain(DecisionJudgeId);
        }
    }

    #endregion
}

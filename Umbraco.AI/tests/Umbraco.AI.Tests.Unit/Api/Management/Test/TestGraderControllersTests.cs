// Story DE-4 — Only offer Decision judges when Decision is on (AC2, AC3, AC4, AC5, AC7, AC8, AC9)
//
// Drives the real AllTestGradersController and ByIdTestGraderController actions (the by-id one
// through a real UmbracoMapper + real TestMapDefinition). Uses fake graders, one marked
// [AIRequiresCapability(AICapability.Decision)] under the id "decision-judge", so this doesn't
// depend on the real Decision grader.
//
// AC9 sets StaticServiceProvider.Instance (public setter) to a mock provider for the duration of
// the scenario and restores it after; its collection runs with parallelization disabled.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Tests.Unit.Api.Management;
using Umbraco.AI.Web.Api.Management.Test.Controllers;
using Umbraco.AI.Web.Api.Management.Test.Mapping;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Test;

public class TestGraderControllersTests
{
    private const string DecisionJudgeId = "decision-judge";

    private class FakeGrader(string id) : IAITestGrader
    {
        public string Id { get; } = id;

        public string Name => Id;

        public string Description => Id;

        public AIGraderType Type => AIGraderType.CodeBased;

        public Type? ConfigType => null;

        public AIEditableModelSchema? GetConfigSchema() => null;

        public Task<AITestGraderResult> GradeAsync(
            AITestTranscript transcript,
            AITestOutcome outcome,
            AITestGraderConfig graderConfig,
            CancellationToken cancellationToken)
            => Task.FromResult(new AITestGraderResult { GraderId = graderConfig.Id, Passed = true, Score = 1 });
    }

    [AIRequiresCapability(AICapability.Decision)]
    private sealed class FakeDecisionGrader() : FakeGrader(DecisionJudgeId);

    private static AITestGraderCollection Graders()
        => new(() => [new FakeGrader("exact-match"), new FakeGrader("contains"), new FakeDecisionGrader()]);

    private static IUmbracoMapper Mapper()
        => new UmbracoMapper(
            new MapDefinitionCollection(() => [new TestMapDefinition()]),
            Mock.Of<Umbraco.Cms.Core.Scoping.ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);

    private static Mock<IAIExperimentalFeatures> FlagMock(Func<bool> decisionOn)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(It.IsAny<AICapability>()))
            .Returns((AICapability c) => c != AICapability.Decision || decisionOn());
        return experimental;
    }

    private static IAIExperimentalFeatures Flag(bool decisionOn) => FlagMock(() => decisionOn).Object;

    private static IReadOnlyList<string> ListIds(AllTestGradersController controller)
        => ((IEnumerable<TestGraderInfoModel>)((OkObjectResult)controller.GetAllTestGraders().Result!).Value!)
            .Select(g => g.Id)
            .ToList();

    private static IReadOnlyList<string> ListIds(bool decisionOn)
        => ListIds(new AllTestGradersController(Graders(), Flag(decisionOn)));

    private static IActionResult GetById(bool decisionOn)
        => new ByIdTestGraderController(Graders(), Mapper(), Flag(decisionOn))
            .GetTestGraderById(DecisionJudgeId).GetAwaiter().GetResult();

    #region Happy path

    public class GivenTheFlagOn
    {
        [Fact]
        public void ListsTheDecisionJudge() => ListIds(decisionOn: true).ShouldContain(DecisionJudgeId);

        [Fact]
        public void GetByIdReturns200() => GetById(decisionOn: true).ShouldBeOfType<OkObjectResult>();

        [Fact]
        public void GetByIdReturnsTheGrader()
            => ((TestGraderResponseModel)((OkObjectResult)GetById(decisionOn: true)).Value!).Id.ShouldBe(DecisionJudgeId);
    }

    public class GivenTheFlagOffAndOtherGraders
    {
        [Fact]
        public void StillListsEveryOtherGrader()
            => new[] { "exact-match", "contains" }.ShouldBeSubsetOf(ListIds(decisionOn: false));
    }

    public class GivenTheFlagFlippedOnAtRuntime
    {
        [Fact]
        public void ListsTheDecisionJudgeOnTheNextRequest()
        {
            var decisionOn = false;
            var controller = new AllTestGradersController(Graders(), FlagMock(() => decisionOn).Object);
            ListIds(controller);

            decisionOn = true;

            ListIds(controller).ShouldContain(DecisionJudgeId);
        }

        [Fact]
        public void GetByIdFindsTheDecisionJudgeOnTheNextRequest()
        {
            var decisionOn = false;
            var controller = new ByIdTestGraderController(Graders(), Mapper(), FlagMock(() => decisionOn).Object);
            controller.GetTestGraderById(DecisionJudgeId).GetAwaiter().GetResult();

            decisionOn = true;

            controller.GetTestGraderById(DecisionJudgeId).GetAwaiter().GetResult().ShouldBeOfType<OkObjectResult>();
        }
    }

    #endregion

    #region Sad path

    public class GivenTheFlagOff
    {
        [Fact]
        public void OmitsTheDecisionJudge() => ListIds(decisionOn: false).ShouldNotContain(DecisionJudgeId);

        [Fact]
        public void GetByIdReturns404() => GetById(decisionOn: false).ShouldBeOfType<NotFoundObjectResult>();
    }

    [Collection(nameof(StaticServiceProviderTestCollection))]
    public class GivenControllersBuiltThroughTheirObsoleteConstructors : IDisposable
    {
        private readonly IServiceProvider? _previous = StaticServiceProvider.Instance;

        public GivenControllersBuiltThroughTheirObsoleteConstructors()
        {
            var services = new Mock<IServiceProvider>();
            services.Setup(x => x.GetService(typeof(IAIExperimentalFeatures))).Returns(Flag(decisionOn: false));
            StaticServiceProvider.Instance = services.Object;
        }

        public void Dispose() => StaticServiceProvider.Instance = _previous!;

#pragma warning disable CS0618 // Exercises the obsolete constructors on purpose
        [Fact]
        public void TheListFiltersTheSameWay()
            => ListIds(new AllTestGradersController(Graders())).ShouldNotContain(DecisionJudgeId);

        [Fact]
        public void GetByIdFiltersTheSameWay()
            => new ByIdTestGraderController(Graders(), Mapper())
                .GetTestGraderById(DecisionJudgeId).GetAwaiter().GetResult()
                .ShouldBeOfType<NotFoundObjectResult>();
#pragma warning restore CS0618
    }

    #endregion
}

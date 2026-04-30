using System.Text.Json;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Tests.Unit.Tests;

/// <summary>
/// Verifies <see cref="AITestRunner"/> preserves the null-vs-empty distinction when flowing stored
/// test <see cref="AITest.ContextIds"/> into the feature's <c>contextIdsOverride</c> slot.
/// Regression test: an empty stored list used to reach the resolvers as "replace profile contexts
/// with nothing" and suppress them, so a test with only a profile override lost the override
/// profile's contexts.
/// </summary>
public class AITestRunnerTests
{
    private readonly Mock<IAITestRunRepository> _runRepositoryMock = new();
    private readonly Mock<IAITestTranscriptRepository> _transcriptRepositoryMock = new();

    private AITestRunner CreateRunner(RecordingTestFeature feature)
    {
        var features = new AITestFeatureCollection(() => [feature]);
        var graders = new AITestGraderCollection(() => []);
        return new AITestRunner(
            _runRepositoryMock.Object,
            _transcriptRepositoryMock.Object,
            features,
            graders);
    }

    private static AITest BuildTest(IReadOnlyList<Guid> contextIds, params AITestVariation[] variations) => new()
    {
        Id = Guid.NewGuid(),
        Alias = "t",
        Name = "t",
        TestFeatureId = "recording",
        TestTargetId = Guid.NewGuid(),
        ContextIds = contextIds,
        Variations = variations,
        RunCount = 1,
    };

    [Fact]
    public async Task ExecuteTestAsync_NoOverrideAndEmptyStoredContexts_PassesNullToFeature()
    {
        var feature = new RecordingTestFeature();
        var runner = CreateRunner(feature);

        await runner.ExecuteTestAsync(BuildTest([]));

        feature.ReceivedContextIdsOverride.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteTestAsync_NoOverrideAndPopulatedStoredContexts_PassesStoredListToFeature()
    {
        var stored = Guid.NewGuid();
        var feature = new RecordingTestFeature();
        var runner = CreateRunner(feature);

        await runner.ExecuteTestAsync(BuildTest([stored]));

        feature.ReceivedContextIdsOverride.ShouldNotBeNull();
        feature.ReceivedContextIdsOverride!.ShouldBe([stored]);
    }

    [Fact]
    public async Task ExecuteTestAsync_CallerOverridePresent_PassesCallerListToFeatureEvenIfEmpty()
    {
        // An explicit empty caller override means "explicit zero contexts" (mirrors builder SetContexts([])),
        // and should reach the feature so the resolvers receive the override signal.
        var feature = new RecordingTestFeature();
        var runner = CreateRunner(feature);

        await runner.ExecuteTestAsync(BuildTest([Guid.NewGuid()]), contextIdsOverride: []);

        feature.ReceivedContextIdsOverride.ShouldNotBeNull();
        feature.ReceivedContextIdsOverride!.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteTestAsync_VariationWithNoContextIds_FallsBackToNullWhenTestStoredEmpty()
    {
        var feature = new RecordingTestFeature();
        var runner = CreateRunner(feature);

        var variation = new AITestVariation { Id = Guid.NewGuid(), Name = "v", ContextIds = null };
        await runner.ExecuteTestAsync(BuildTest([], variation));

        // Two runs captured: default + variation, both should have null (no override)
        feature.AllReceivedContextIdsOverrides.Count.ShouldBe(2);
        feature.AllReceivedContextIdsOverrides.ShouldAllBe(x => x == null);
    }

    [Fact]
    public async Task ExecuteTestAsync_VariationWithExplicitEmptyContextIds_PassesEmptyOverride()
    {
        var feature = new RecordingTestFeature();
        var runner = CreateRunner(feature);

        var variation = new AITestVariation { Id = Guid.NewGuid(), Name = "v", ContextIds = [] };
        await runner.ExecuteTestAsync(BuildTest([Guid.NewGuid()], variation));

        // Default run uses test's stored list; variation run uses the explicit [] override.
        feature.AllReceivedContextIdsOverrides.Count.ShouldBe(2);
        feature.AllReceivedContextIdsOverrides[1].ShouldNotBeNull();
        feature.AllReceivedContextIdsOverrides[1]!.ShouldBeEmpty();
    }

    private sealed class RecordingTestFeature : IAITestFeature
    {
        public string Id => "recording";
        public string Name => "Recording";
        public string Description => "Captures the overrides it received for assertions";
        public string Category => "test";
        public Type? ConfigType => null;
        public AIEditableModelSchema? GetConfigSchema() => null;
        public string ExtractOutputValue(AITestTranscript transcript) => string.Empty;

        public List<IReadOnlyList<Guid>?> AllReceivedContextIdsOverrides { get; } = [];
        public IReadOnlyList<Guid>? ReceivedContextIdsOverride => AllReceivedContextIdsOverrides.Count > 0
            ? AllReceivedContextIdsOverrides[0]
            : null;

        public Task<AITestTranscript> ExecuteAsync(
            AITest test,
            int runNumber,
            Guid? profileIdOverride,
            IEnumerable<Guid>? contextIdsOverride,
            IEnumerable<Guid>? guardrailIdsOverride,
            CancellationToken cancellationToken)
        {
            AllReceivedContextIdsOverrides.Add(contextIdsOverride?.ToList());
            return Task.FromResult(new AITestTranscript
            {
                RunId = Guid.Empty,
                FinalOutput = JsonDocument.Parse("{}").RootElement,
            });
        }
    }
}

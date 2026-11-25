// GP-10 (AC4); architecture guess 5

using System.Text.Json;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.AGUI;
using Umbraco.AI.Agent.Core.Tests;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tests;

using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Agents;

/// <summary>
/// Agent test runs check the agent limit against the person running the test.
/// </summary>
public class AgentTestRunUsageLimitTests
{
    private static readonly Guid Editors = Guid.NewGuid();
    private static readonly Guid Translators = Guid.NewGuid();

    #region Happy path

    [Fact]
    public async Task TestRun_ByUserInsideGroups_RunsTheAgent()
    {
        // Given a test of an agent limited to editors and a user in editors
        var harness = new AgentUsageLimitTests.Harness(inGroups: [Editors]);
        var agent = harness.Add("limited", allowedGroups: [Editors]);

        // When they run the test
        var transcript = await ExecuteTestAsync(harness, agent);

        // Then the agent run isn't refused
        Status(transcript).ShouldBe("success");
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task TestRun_ByUserOutsideGroups_IsRefused()
    {
        // Given a test of an agent limited to editors and a user with Uai.Test.Manage not in it
        var harness = new AgentUsageLimitTests.Harness(inGroups: [Translators]);
        var agent = harness.Add("limited", allowedGroups: [Editors]);

        // When they run the test
        var transcript = await ExecuteTestAsync(harness, agent);

        // Then the agent run is refused (AC4)
        Status(transcript).ShouldBe("error");
        transcript.FinalOutput.GetProperty("error").GetString()
            .ShouldBe(new AIAgentNotAvailableException(agent.Id).Message);
    }

    #endregion

    private static string? Status(AITestTranscript transcript)
        => transcript.Timing!.Value.GetProperty("status").GetString();

    /// <summary>
    /// Runs the real <see cref="AgentTestFeature"/> against the harness's real agent service.
    /// </summary>
    private static Task<AITestTranscript> ExecuteTestAsync(AgentUsageLimitTests.Harness harness, AIAgent agent)
    {
        var config = new AgentTestFeatureConfig { Message = "Hello" };
        var modelResolver = new Mock<IAIEditableModelResolver>();
        modelResolver
            .Setup(x => x.ResolveModel(typeof(AgentTestFeatureConfig), It.IsAny<object?>(), It.IsAny<AIEditableModelSchema?>()))
            .Returns(config);

        var infrastructure = new Mock<IAITestFeatureInfrastructure>();
        infrastructure.SetupGet(x => x.ContextResolver).Returns(new AITestContextResolver());
        infrastructure.SetupGet(x => x.SchemaBuilder).Returns(Mock.Of<IAIEditableModelSchemaBuilder>());
        infrastructure.SetupGet(x => x.ModelResolver).Returns(modelResolver.Object);

        var scope = new Mock<IAIRuntimeContextScope>();
        scope.SetupGet(x => x.Context).Returns(new AIRuntimeContext([]));
        var scopeProvider = new Mock<IAIRuntimeContextScopeProvider>();
        scopeProvider.Setup(x => x.CreateScope(It.IsAny<IEnumerable<AIRequestContextItem>>())).Returns(scope.Object);

        var contextConverter = new Mock<IAGUIContextConverter>();
        contextConverter
            .Setup(x => x.ConvertToRequestContextItems(It.IsAny<IEnumerable<Umbraco.AI.AGUI.Models.AGUIContextItem>?>()))
            .Returns([]);

        var feature = new AgentTestFeature(
            harness.Service,
            contextConverter.Object,
            scopeProvider.Object,
            new AIRuntimeContextContributorCollection(() => []),
            infrastructure.Object);

        var test = new AITest
        {
            Alias = "agent-test",
            Name = "Agent test",
            TestFeatureId = "agent",
            TestTargetId = agent.Id,
            TestFeatureConfig = JsonSerializer.SerializeToElement(new { message = "Hello" }),
        };

        return feature.ExecuteAsync(test, 1, null, null, null, CancellationToken.None);
    }
}

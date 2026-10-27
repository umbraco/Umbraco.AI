using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Deploy.Artifacts;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Connectors.ServiceConnectors;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Xunit;

namespace Umbraco.AI.Deploy.Tests.Unit.Connectors.ServiceConnectors;

public class UmbracoAIGuardrailServiceConnectorTests
{
    private readonly Mock<IAIGuardrailService> _guardrailServiceMock = new();
    private readonly UmbracoAIGuardrailServiceConnector _connector;
    private AIGuardrail? _saved;

    public UmbracoAIGuardrailServiceConnectorTests()
    {
        var settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());
        _guardrailServiceMock
            .Setup(x => x.SaveGuardrailAsync(It.IsAny<AIGuardrail>(), It.IsAny<CancellationToken>()))
            .Returns<AIGuardrail, CancellationToken>((g, _) =>
            {
                _saved = g;
                return Task.FromResult(g);
            });
        _connector = new UmbracoAIGuardrailServiceConnector(
            _guardrailServiceMock.Object,
            settingsAccessorMock.Object,
            new AIGuardrailEvaluatorCollection(() => []));
    }

    [Fact]
    public async Task RoundTrip_KeepsRuleIdsAndProperties()
    {
        var guardrailId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var source = new AIGuardrail(guardrailId)
        {
            Alias = "safety",
            Name = "Safety",
            Rules =
            [
                new AIGuardrailRule(firstId)
                {
                    EvaluatorId = "pii",
                    Name = "No PII",
                    Phase = AIGuardrailPhase.PreGenerate,
                    Action = AIGuardrailAction.Warn,
                    SortOrder = 1,
                    Config = JsonDocument.Parse("""{"threshold":0.5}""").RootElement,
                },
                new AIGuardrailRule(secondId) { EvaluatorId = "toxicity", Name = "Kind", SortOrder = 2 },
            ],
        };

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, guardrailId);
        var artifact = await _connector.GetArtifactAsync(udi, source);
        await ImportAsync(artifact!);

        _saved!.Rules.Select(r => r.Id).ShouldBe([firstId, secondId]);
        var first = _saved.Rules[0];
        first.EvaluatorId.ShouldBe("pii");
        first.Name.ShouldBe("No PII");
        first.Phase.ShouldBe(AIGuardrailPhase.PreGenerate);
        first.Action.ShouldBe(AIGuardrailAction.Warn);
        first.SortOrder.ShouldBe(1);
        first.Config!.Value.GetProperty("threshold").GetDouble().ShouldBe(0.5);
    }

    [Fact]
    public async Task Import_LegacyArtifactWithoutRuleIds_LeavesIdsEmpty()
    {
        var artifact = new AIGuardrailArtifact(new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, Guid.NewGuid()))
        {
            Alias = "safety",
            Name = "Safety",
            Rules = JsonDocument.Parse("""[{"EvaluatorId":"pii","Name":"No PII","SortOrder":0}]""").RootElement,
        };

        await ImportAsync(artifact);

        _saved!.Rules.Single().Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task Export_SensitiveConfigLiteral_IsNotWritten()
    {
        var connector = CreateConnectorWithSensitiveEvaluator();
        var ruleId = Guid.NewGuid();
        var source = new AIGuardrail(Guid.NewGuid())
        {
            Alias = "safety",
            Name = "Safety",
            Rules = [new AIGuardrailRule(ruleId) { EvaluatorId = "remote", Name = "Remote", Config = Json("""{"apiKey":"secret","threshold":0.5}""") }],
        };

        var artifact = await connector.GetArtifactAsync(new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, source.Id), source);

        var config = artifact!.Rules!.Value[0].GetProperty("Config");
        config.TryGetProperty("apiKey", out _).ShouldBeFalse();
        config.GetProperty("threshold").GetDouble().ShouldBe(0.5);
    }

    [Fact]
    public async Task Import_SensitiveConfigLiteral_IsNotImported_AndTargetValueIsKept()
    {
        var connector = CreateConnectorWithSensitiveEvaluator();
        var guardrailId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();
        var existing = new AIGuardrail(guardrailId)
        {
            Alias = "safety",
            Name = "Safety",
            Rules = [new AIGuardrailRule(ruleId) { EvaluatorId = "remote", Name = "Remote", Config = Json("""{"apiKey":"target-secret"}""") }],
        };
        var artifact = new AIGuardrailArtifact(new GuidUdi(UmbracoAIConstants.UdiEntityType.Guardrail, guardrailId))
        {
            Alias = "safety",
            Name = "Safety",
            Rules = Json($$$"""[{"Id":"{{{ruleId}}}","EvaluatorId":"remote","Name":"Remote","Config":{"apiKey":"leaked","threshold":0.7}}]"""),
        };

        var state = new ArtifactDeployState<AIGuardrailArtifact, AIGuardrail>(artifact, existing, connector, 2);
        await connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);

        var config = _saved!.Rules.Single().Config!.Value;
        config.GetProperty("apiKey").GetString().ShouldBe("target-secret");
        config.GetProperty("threshold").GetDouble().ShouldBe(0.7);
    }

    private UmbracoAIGuardrailServiceConnector CreateConnectorWithSensitiveEvaluator()
    {
        var evaluator = new Mock<IAIGuardrailEvaluator>();
        evaluator.SetupGet(x => x.Id).Returns("remote");
        evaluator.Setup(x => x.GetConfigSchema()).Returns(new AIEditableModelSchema(typeof(object),
        [
            new AIEditableModelField { Key = "apiKey", Label = "API Key", IsSensitive = true },
            new AIEditableModelField { Key = "threshold", Label = "Threshold" },
        ]));

        var settingsAccessorMock = new Mock<UmbracoAIDeploySettingsAccessor>(MockBehavior.Strict, null!);
        settingsAccessorMock.Setup(x => x.Settings).Returns(new UmbracoAIDeploySettings());

        return new UmbracoAIGuardrailServiceConnector(
            _guardrailServiceMock.Object,
            settingsAccessorMock.Object,
            new AIGuardrailEvaluatorCollection(() => [evaluator.Object]));
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private Task ImportAsync(AIGuardrailArtifact artifact)
    {
        var state = new ArtifactDeployState<AIGuardrailArtifact, AIGuardrail>(artifact, null, _connector, 2);
        return _connector.ProcessAsync(state, Mock.Of<IDeployContext>(), 2);
    }
}

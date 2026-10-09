// DR-16 — Ask several questions in one Automate step (AC5, AC6, AC8, AC9 run-time guard).
//
// All assumptions in the original header matched the real signatures exactly. One fix was
// needed: SchemaAsync() went through IStepType.GetOutputSchemaAsync(Dictionary<string, object?>),
// which resolves settings via ActionInfrastructure.ModelResolver — a bare Mock<IEditableModelResolver>
// (as used by the other decision action test files, which never exercise this path) returns null
// for an unconfigured ResolveModel<T> call. Fixed by giving the schema tests a resolver stub that
// round-trips through System.Text.Json the same way the real EditableModelResolver does for an
// already-JSON-shaped input (Umbraco.Automate.Core's RunScriptActionTests uses the real internal
// EditableModelResolver type directly for the same reason, which isn't visible outside that
// product's own test assembly — see CreateRoundTrippingModelResolver).
//
// Also added: a settings round-trip test proving a JSON payload in the editor's exact camelCase
// shape deserializes correctly using Automate's settings JSON convention (camelCase,
// case-insensitive) — the concrete resolver and its internal JsonSerializerOptions aren't visible
// outside Umbraco.Automate.Core, so this asserts the equivalent public-surface behavior directly.
//
// Core is the single source of the per-kind option/level bounds; the one-option case checks that
// its ArgumentException maps to Validation.
using System.Text.Json;
using Json.Schema;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.StepTypes;
using Xunit;

#pragma warning disable UMBRACOAI_DECISION

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

public class AskDecisionsActionTests
{
    private readonly Mock<IAIDecisionService> _decisionServiceMock = new();
    private readonly Mock<IAIExperimentalFeatures> _experimentalMock = new();
    private readonly ActionInfrastructure _infrastructure = new(new Mock<IEditableModelResolver>().Object);

    public AskDecisionsActionTests()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(true);
        _decisionServiceMock
            .Setup(s => s.GetDecisionResponseAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIDecisionResponse
            {
                Answers = new Dictionary<string, AIDecisionAnswer>
                {
                    ["refund"] = new AIBinaryDecisionAnswer { TrueProbability = 0.97 },
                    ["category"] = new AIChoiceDecisionAnswer
                    {
                        Choice = "billing",
                        Confidence = 0.8,
                        Probabilities = new Dictionary<string, double> { ["billing"] = 0.8, ["shipping"] = 0.2 },
                    },
                    ["mood"] = new AIScoreDecisionAnswer
                    {
                        Score = 1.6,
                        Confidence = 0.7,
                        Probabilities = new Dictionary<int, double> { [0] = 0.1, [1] = 0.2, [2] = 0.7 },
                    },
                },
            });
    }

    private static List<AskDecisionsQuestion> ThreeQuestions() =>
    [
        new() { Kind = "binary", Alias = "refund", Instructions = "Refund requested?", Threshold = 0.5 },
        new()
        {
            Kind = "choice",
            Alias = "category",
            Instructions = "What about?",
            Options = [new AskChoiceDecisionOption { Key = "billing" }, new AskChoiceDecisionOption { Key = "shipping" }],
        },
        new() { Kind = "score", Alias = "mood", Instructions = "How frustrated?", Levels = ["calm", "concerned", "angry"] },
    ];

    #region Scenario: three questions run in one step

    [Fact]
    public async Task MakesExactlyOneDecisionCall()
    {
        await RunAsync(new AskDecisionsSettings { Context = "My order", Questions = ThreeQuestions() });

        _decisionServiceMock.Verify(
            s => s.GetDecisionResponseAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendsContextAsState()
    {
        AIDecisionRequest? sent = null;
        _decisionServiceMock
            .Setup(s => s.GetDecisionResponseAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIDecisionBuilder>, AIDecisionRequest, CancellationToken>((_, r, _) => sent = r)
            .ReturnsAsync(new AIDecisionResponse { Answers = new Dictionary<string, AIDecisionAnswer>() });

        await RunAsync(new AskDecisionsSettings { Context = "My order", Questions = ThreeQuestions() });

        sent!.State.ShouldBe("My order");
    }

    [Fact]
    public async Task OutputsTheYesNoAnswer()
        => (await OutputAsync()).GetProperty("refund").GetProperty("answer").GetBoolean().ShouldBeTrue();

    [Fact]
    public async Task OutputsTheYesNoProbability()
        => (await OutputAsync()).GetProperty("refund").GetProperty("probability").GetDouble().ShouldBe(0.97);

    [Fact]
    public async Task OutputsTheChoice()
        => (await OutputAsync()).GetProperty("category").GetProperty("choice").GetString().ShouldBe("billing");

    [Fact]
    public async Task OutputsTheChoiceConfidence()
        => (await OutputAsync()).GetProperty("category").GetProperty("confidence").GetDouble().ShouldBe(0.8);

    [Fact]
    public async Task OutputsTheScore()
        => (await OutputAsync()).GetProperty("mood").GetProperty("score").GetDouble().ShouldBe(1.6);

    [Fact]
    public async Task OutputsTheNearestLevelLabel()
        => (await OutputAsync()).GetProperty("mood").GetProperty("level").GetString().ShouldBe("angry");

    [Fact]
    public async Task OutputsTheScoreConfidence()
        => (await OutputAsync()).GetProperty("mood").GetProperty("confidence").GetDouble().ShouldBe(0.7);

    #endregion

    #region Scenario: the output schema follows the configured questions

    [Fact]
    public async Task OutputSchemaListsEachAlias()
    {
        var schema = await SchemaAsync();

        schema!.GetProperties()!.Keys.ShouldBe(["refund", "category", "mood"], ignoreOrder: true);
    }

    [Fact]
    public async Task OutputSchemaDescribesYesNoFields()
        => (await SchemaAsync())!.GetProperties()!["refund"].GetProperties()!.Keys.ShouldBe(["answer", "probability"], ignoreOrder: true);

    [Fact]
    public async Task OutputSchemaDescribesPickOneFields()
        => (await SchemaAsync())!.GetProperties()!["category"].GetProperties()!.Keys.ShouldBe(["choice", "confidence"], ignoreOrder: true);

    [Fact]
    public async Task OutputSchemaDescribesScoreFields()
        => (await SchemaAsync())!.GetProperties()!["mood"].GetProperties()!.Keys.ShouldBe(["score", "level", "confidence"], ignoreOrder: true);

    #endregion

    #region Sad path: invalid settings

    public static TheoryData<string, List<AskDecisionsQuestion>> InvalidQuestionSets => new()
    {
        { "no questions", [] },
        {
            "more than 20 questions",
            Enumerable.Range(0, 21).Select(i => new AskDecisionsQuestion { Kind = "binary", Alias = $"q{i}", Instructions = "?" }).ToList()
        },
        {
            "a duplicate alias",
            [
                new() { Kind = "binary", Alias = "same", Instructions = "One?" },
                new() { Kind = "binary", Alias = "same", Instructions = "Two?" },
            ]
        },
        {
            "an alias starting with a digit",
            [new() { Kind = "binary", Alias = "1refund", Instructions = "?" }]
        },
        {
            "an alias containing a space",
            [new() { Kind = "binary", Alias = "re fund", Instructions = "?" }]
        },
        {
            "an alias containing a dot",
            [new() { Kind = "binary", Alias = "a.b", Instructions = "?" }]
        },
        {
            "an unknown kind",
            [new() { Kind = "ranking", Alias = "mood", Instructions = "?" }]
        },
        {
            "a threshold above 1.0",
            [new() { Kind = "binary", Alias = "refund", Instructions = "?", Threshold = 1.5 }]
        },
        {
            "a NaN threshold",
            [new() { Kind = "binary", Alias = "refund", Instructions = "?", Threshold = double.NaN }]
        },
    };

    [Theory]
    [MemberData(nameof(InvalidQuestionSets))]
    public async Task InvalidSettings_FailWithValidation(string _, List<AskDecisionsQuestion> questions)
    {
        var result = await RunAsync(new AskDecisionsSettings { Context = "text", Questions = questions });

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Theory]
    [MemberData(nameof(InvalidQuestionSets))]
    public async Task InvalidSettings_DoNotCallTheProvider(string _, List<AskDecisionsQuestion> questions)
    {
        await RunAsync(new AskDecisionsSettings { Context = "text", Questions = questions });

        _decisionServiceMock.Verify(
            s => s.GetDecisionResponseAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Sad path: a pick-one with 1 option (rejected by Core's Decision validator)

    [Fact]
    public async Task AskDecisions_WithOneOption_FailsWithValidation()
    {
        // Core's ValidatingDecisionClient (via the shared DecisionQuestionValidator) is the single
        // source of the 2..255 option-count rule, and throws ArgumentException before any provider
        // call — the action just maps that to Validation, same as AskChoiceDecisionAction does
        // (see DecisionActionsTests.AskChoice_WithOneOption_FailsWithValidation).
        _decisionServiceMock
            .Setup(s => s.GetDecisionResponseAsync(It.IsAny<Action<AIDecisionBuilder>>(), It.IsAny<AIDecisionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Options must contain between 2 and 255 entries.", "request"));

        var result = await RunAsync(new AskDecisionsSettings
        {
            Context = "text",
            Questions = [new() { Kind = "choice", Alias = "pick", Instructions = "Which?", Options = [new AskChoiceDecisionOption { Key = "a" }] }],
        });

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    #region Sad path: flag turned off after startup

    [Fact]
    public async Task WhenFlagOff_FailsWithValidation()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        var result = await RunAsync(new AskDecisionsSettings { Context = "text", Questions = ThreeQuestions() });

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    private AskDecisionsAction CreateAction()
        => new(_infrastructure, _decisionServiceMock.Object, _experimentalMock.Object, Mock.Of<ILogger<AskDecisionsAction>>());

    private Task<ActionResult> RunAsync(AskDecisionsSettings settings)
        => CreateAction().ExecuteAsync(
            new ActionContext
            {
                AutomationId = Guid.NewGuid(),
                RunId = Guid.NewGuid(),
                StepId = Guid.NewGuid(),
                ActionAlias = UmbracoAIAutomateConstants.ActionTypes.AskDecisions,
                Settings = settings,
            },
            CancellationToken.None);

    private async Task<JsonElement> OutputAsync()
    {
        var result = await RunAsync(new AskDecisionsSettings { Context = "My order", Questions = ThreeQuestions() });
        return JsonSerializer.SerializeToElement(result.OutputData, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    /// <summary>
    /// Through <see cref="IStepType"/>, as Automate's catalogue resolves it (see
    /// RunScriptActionTests). That path resolves settings via
    /// <c>ActionInfrastructure.ModelResolver</c>, so — unlike <see cref="CreateAction"/>'s bare
    /// <see cref="Mock{T}"/> — this needs a resolver that actually round-trips the dictionary
    /// into a typed <see cref="AskDecisionsSettings"/>, the same way the real (internal, not
    /// visible outside Umbraco.Automate.Core) <c>EditableModelResolver</c> does for an
    /// already-JSON-shaped input.
    /// </summary>
    private static Task<JsonSchema?> SchemaAsync()
    {
        // GetOutputSchemaAsync touches neither the decision service nor the experimental flag
        // (the flag is only checked by ExecuteAsync), so these two don't need any setup.
        IStepType action = new AskDecisionsAction(
            new ActionInfrastructure(CreateRoundTrippingModelResolver()),
            Mock.Of<IAIDecisionService>(),
            Mock.Of<IAIExperimentalFeatures>(),
            Mock.Of<ILogger<AskDecisionsAction>>());

        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            JsonSerializer.Serialize(new AskDecisionsSettings { Questions = ThreeQuestions() }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return action.GetOutputSchemaAsync(settings);
    }

    private static IEditableModelResolver CreateRoundTrippingModelResolver()
    {
        var resolver = new Mock<IEditableModelResolver>();
        resolver
            .Setup(r => r.ResolveModel<AskDecisionsSettings>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<EditableModelSchema?>()))
            .Returns((string _, object? data, EditableModelSchema? _) => data is null
                ? null
                : JsonSerializer.Deserialize<AskDecisionsSettings>(
                    JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }));
        return resolver.Object;
    }

    #region Scenario: settings round-trip the editor's exact camelCase JSON shape

    private const string EditorShapedJson = """
        {
            "context": "My order",
            "profileId": "11111111-1111-1111-1111-111111111111",
            "questions": [
                { "kind": "binary", "alias": "refund", "instructions": "Refund requested?", "trueCriteria": "clearly owed", "falseCriteria": "no evidence", "threshold": 0.75 },
                { "kind": "choice", "alias": "category", "instructions": "What about?", "options": [{ "key": "billing", "value": "Billing" }, { "key": "shipping" }] },
                { "kind": "score", "alias": "mood", "instructions": "How frustrated?", "levels": ["calm", "concerned", "angry"] }
            ]
        }
        """;

    // Automate's own settings convention: camelCase, case-insensitive (see the class remarks
    // above on why the real internal JsonOptions.Settings instance can't be referenced directly).
    private static readonly JsonSerializerOptions EditorShapedJsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    [Fact]
    public void SettingsDeserialize_AllThreeQuestionKinds()
        => JsonSerializer.Deserialize<AskDecisionsSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Questions.Select(q => q.Kind).ShouldBe(["binary", "choice", "score"]);

    [Fact]
    public void SettingsDeserialize_TheBindableContext()
        => JsonSerializer.Deserialize<AskDecisionsSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Context.ShouldBe("My order");

    [Fact]
    public void SettingsDeserialize_TheProfilePickersGuid()
        => JsonSerializer.Deserialize<AskDecisionsSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .ProfileId.ShouldBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    [Fact]
    public void SettingsDeserialize_TheBinaryThreshold()
        => JsonSerializer.Deserialize<AskDecisionsSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Questions.Single(q => q.Alias == "refund").Threshold.ShouldBe(0.75);

    [Fact]
    public void SettingsDeserialize_TheChoiceOptionsKeyValueRows()
        => JsonSerializer.Deserialize<AskDecisionsSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Questions.Single(q => q.Alias == "category").Options!.Select(o => (o.Key, o.Value))
            .ShouldBe([("billing", "Billing"), ("shipping", null)]);

    [Fact]
    public void SettingsDeserialize_TheScoreLevels()
        => JsonSerializer.Deserialize<AskDecisionsSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Questions.Single(q => q.Alias == "mood").Levels.ShouldBe(["calm", "concerned", "angry"]);

    #endregion
}

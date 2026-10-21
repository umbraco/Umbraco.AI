// EX-10 — Extract values in an automation (also the server half of EX-9 AC11 and AC18).
//
// Copies AskDecisionsActionTests' style: xUnit + Moq + Shouldly, flat class, #region per scenario,
// the round-tripping model resolver for the output schema path.
//
// Assumed shapes (from ARCHITECTURE.md decisions 1, 4 and 8; rename here if the build differs):
// - ExtractValuesAction(ActionInfrastructure, IAIExtractionService, IAIExperimentalFeatures,
//   TimeProvider, ILogger<ExtractValuesAction>). TimeProvider is injected so "empty reference date
//   uses the run date (UTC)" can be pinned without a midnight race.
// - ExtractValuesSettings { List<ExtractValuesField> Fields; string? Context; string? ReferenceDate; Guid? ProfileId }
// - ExtractValuesField { string Kind; string Alias; string Instructions; int? Decimals; bool Integer;
//   string? Unit; string? ItemType }. Kind is one of text|number|date|time|datetime|duration|list,
//   matching the frontend field list editor's `kind`.
// - The action calls IAIExtractionService.GetExtractionResponseAsync(Action<AIExtractionBuilder>,
//   AIExtractionRequest, CancellationToken), like AskDecisionsAction's builder overload.
// - UmbracoAIAutomateConstants.ActionTypes.ExtractValues is the action alias.
//
// Live-only (checked in wire tasks T18/T19, not here): AC1 and AC15 (the action picker, which
// depends on the compose-time Exclude<> — this project has no composer test host), and AC14
// (an If step evaluating the output inside Automate's engine).
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Settings;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Umbraco.Automate.Core.StepTypes;
using Xunit;

#pragma warning disable UMBRACOAI_EXTRACTION

namespace Umbraco.AI.Automate.Tests.Unit.Extraction;

public class ExtractValuesActionTests
{
    private static readonly DateTimeOffset RunTime = new(2026, 10, 20, 23, 30, 0, TimeSpan.Zero);
    private static readonly Guid PickedProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string OrderText = "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.";

    private readonly Mock<IAIExtractionService> _extractionServiceMock = new();
    private readonly Mock<IAIExperimentalFeatures> _experimentalMock = new();
    private readonly ActionInfrastructure _infrastructure = new(new Mock<IEditableModelResolver>().Object);

    public ExtractValuesActionTests()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(true);
        _extractionServiceMock
            .Setup(s => s.GetExtractionResponseAsync(It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["orderId"] = new AITextExtractionAnswer { Value = "A-88412", Span = new AIExtractionSpan(6, 13), Confidence = 0.9999 },
                    ["amount"] = new AINumberExtractionAnswer { Value = 701.10m, Confidence = 0.99 },
                    ["deliveredOn"] = new AIDateExtractionAnswer { Value = new DateOnly(2026, 10, 3), Confidence = 0.998 },
                    ["returnWindow"] = new AIDurationExtractionAnswer { Value = 14m, Unit = AIDurationUnit.Days, Confidence = 0.9 },
                    ["damaged"] = new AIListExtractionAnswer
                    {
                        Values = ["mug", "teapot"],
                        Spans = [new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)],
                        Confidence = 0.99,
                    },
                    ["invoiceNo"] = new AITextExtractionAnswer { Value = null },
                },
            });
    }

    private static List<ExtractValuesField> OrderFields() =>
    [
        new() { Kind = "text", Alias = "orderId", Instructions = "The order number" },
        new() { Kind = "number", Alias = "amount", Instructions = "Amount paid", Decimals = 2, Unit = "DKK" },
        new() { Kind = "date", Alias = "deliveredOn", Instructions = "Delivery date" },
        new() { Kind = "duration", Alias = "returnWindow", Instructions = "Return window", Unit = "days" },
        new() { Kind = "list", Alias = "damaged", Instructions = "Damaged products", ItemType = "text" },
        new() { Kind = "text", Alias = "invoiceNo", Instructions = "The invoice number" },
    ];

    private static ExtractValuesSettings OrderSettings() => new() { Context = OrderText, Fields = OrderFields() };

    #region Scenario: the action is described as "Extract Values"

    [Fact(Skip = "Pending")]
    public void Action_IsNamedExtractValues()
        => typeof(ExtractValuesAction).GetCustomAttribute<ActionAttribute>()!.Name.ShouldBe("Extract Values");

    [Fact(Skip = "Pending")]
    public void Action_IsInTheAIGroup()
        => typeof(ExtractValuesAction).GetCustomAttribute<ActionAttribute>()!.Group.ShouldBe("AI");

    #endregion

    #region Scenario: settings fields are wired to the right editors

    [Fact(Skip = "Pending")]
    public void Fields_UseTheExtractionFieldListEditor()
        => GetField(nameof(ExtractValuesSettings.Fields)).EditorUiAlias.ShouldBe("Uai.PropertyEditorUi.ExtractionFieldList");

    [Fact(Skip = "Pending")]
    public void Fields_AreCappedAtTwentyInTheEditor()
        => GetField(nameof(ExtractValuesSettings.Fields)).EditorConfig.ShouldBe("""[{ "alias": "max", "value": 20 }]""");

    [Fact(Skip = "Pending")]
    public void Fields_AreNotBindable()
        => GetField(nameof(ExtractValuesSettings.Fields)).SupportsBindings.ShouldBeFalse();

    [Fact(Skip = "Pending")]
    public void Context_SupportsBindings()
        => GetField(nameof(ExtractValuesSettings.Context)).SupportsBindings.ShouldBeTrue();

    [Fact(Skip = "Pending")]
    public void ReferenceDate_SupportsBindings()
        => GetField(nameof(ExtractValuesSettings.ReferenceDate)).SupportsBindings.ShouldBeTrue();

    [Fact(Skip = "Pending")]
    public void Profile_IsFilteredToExtractionProfiles()
        => GetField(nameof(ExtractValuesSettings.ProfileId)).EditorConfig.ShouldBe("""[{ "alias": "capability", "value": "Extraction" }]""");

    #endregion

    #region Scenario: the output schema follows the configured fields

    [Fact(Skip = "Pending")]
    public async Task OutputSchema_ListsEachAlias()
        => (await SchemaJsonAsync(OrderFields())).GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .ShouldBe(["orderId", "amount", "deliveredOn", "returnWindow", "damaged", "invoiceNo"], ignoreOrder: true);

    [Fact(Skip = "Pending")]
    public async Task OutputSchema_GivesEachAliasValueFoundAndConfidence()
        => (await AliasSchemaAsync("text")).GetProperty("properties").EnumerateObject().Select(p => p.Name)
            .ShouldBe(["value", "found", "confidence"], ignoreOrder: true);

    [Theory(Skip = "Pending")]
    [InlineData("text", "string")]
    [InlineData("date", "string")]
    [InlineData("time", "string")]
    [InlineData("datetime", "string")]
    [InlineData("number", "number")]
    [InlineData("duration", "number")]
    [InlineData("list", "array")]
    public async Task OutputSchema_TypesTheValueByFieldKind(string kind, string expectedType)
        => (await AliasSchemaAsync(kind)).GetProperty("properties").GetProperty("value").GetProperty("type").GetString()
            .ShouldBe(expectedType);

    [Fact(Skip = "Pending")]
    public async Task OutputSchema_TypesListItemsAsStrings()
        => (await AliasSchemaAsync("list")).GetProperty("properties").GetProperty("value").GetProperty("items").GetProperty("type").GetString()
            .ShouldBe("string");

    [Fact(Skip = "Pending")]
    public async Task OutputSchema_TypesFoundAsBoolean()
        => (await AliasSchemaAsync("number")).GetProperty("properties").GetProperty("found").GetProperty("type").GetString()
            .ShouldBe("boolean");

    [Fact(Skip = "Pending")]
    public async Task OutputSchema_TypesConfidenceAsNullableNumber()
        => (await AliasSchemaAsync("number")).GetProperty("properties").GetProperty("confidence").GetProperty("type")
            .EnumerateArray().Select(t => t.GetString()).ShouldBe(["number", "null"]);

    #endregion

    #region Scenario: fields, content and reference date are sent in one call

    [Fact(Skip = "Pending")]
    public async Task MakesExactlyOneExtractionCall()
    {
        await RunAsync(OrderSettings());

        _extractionServiceMock.Verify(
            s => s.GetExtractionResponseAsync(It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(Skip = "Pending")]
    public async Task SendsTheResolvedContentAsState()
        => (await CaptureRequestAsync(OrderSettings())).State.ShouldBe(OrderText);

    [Fact(Skip = "Pending")]
    public async Task SendsOneFieldPerConfiguredField()
        => (await CaptureRequestAsync(OrderSettings())).Fields.Count.ShouldBe(6);

    [Fact(Skip = "Pending")]
    public async Task UsesTheAliasAsTheFieldId()
        => (await CaptureRequestAsync(OrderSettings())).Fields.Select(f => f.Id).ShouldBe(
            ["orderId", "amount", "deliveredOn", "returnWindow", "damaged", "invoiceNo"]);

    [Theory(Skip = "Pending")]
    [InlineData("text", typeof(AITextExtractionField))]
    [InlineData("number", typeof(AINumberExtractionField))]
    [InlineData("date", typeof(AIDateExtractionField))]
    [InlineData("time", typeof(AITimeExtractionField))]
    [InlineData("datetime", typeof(AIDateTimeExtractionField))]
    [InlineData("duration", typeof(AIDurationExtractionField))]
    [InlineData("list", typeof(AIListExtractionField))]
    public async Task MapsEachKindToItsCoreFieldType(string kind, Type expectedType)
        => (await CaptureRequestAsync(new ExtractValuesSettings
        {
            Context = OrderText,
            Fields = [new() { Kind = kind, Alias = "f", Instructions = "Something" }],
        })).Fields.Single().ShouldBeOfType(expectedType);

    [Fact(Skip = "Pending")]
    public async Task SendsTheNumberFieldsDecimals()
        => ((AINumberExtractionField)(await CaptureRequestAsync(OrderSettings())).Fields.Single(f => f.Id == "amount")).Decimals.ShouldBe(2);

    [Fact(Skip = "Pending")]
    public async Task SendsTheNumberFieldsUnit()
        => ((AINumberExtractionField)(await CaptureRequestAsync(OrderSettings())).Fields.Single(f => f.Id == "amount")).Unit.ShouldBe("DKK");

    [Fact(Skip = "Pending")]
    public async Task SendsTheDurationFieldsUnit()
        => ((AIDurationExtractionField)(await CaptureRequestAsync(OrderSettings())).Fields.Single(f => f.Id == "returnWindow")).Unit
            .ShouldBe(AIDurationUnit.Days);

    [Fact(Skip = "Pending")]
    public async Task SendsTheListFieldsItemType()
        => ((AIListExtractionField)(await CaptureRequestAsync(new ExtractValuesSettings
        {
            Context = OrderText,
            Fields = [new() { Kind = "list", Alias = "dates", Instructions = "Dates", ItemType = "date" }],
        })).Fields.Single()).ItemType.ShouldBe(AIListItemType.Date);

    #endregion

    #region Scenario: the reference date is bound or defaulted

    [Fact(Skip = "Pending")]
    public async Task SendsABoundIsoDateAsTheReferenceDate()
        => (await CaptureRequestAsync(new ExtractValuesSettings { Context = OrderText, Fields = OrderFields(), ReferenceDate = "2026-10-20" }))
            .ReferenceDate.ShouldBe(new DateOnly(2026, 10, 20));

    [Fact(Skip = "Pending")]
    public async Task SendsTheDatePartOfABoundIsoDateTime()
        => (await CaptureRequestAsync(new ExtractValuesSettings { Context = OrderText, Fields = OrderFields(), ReferenceDate = "2026-10-20T09:15:00Z" }))
            .ReferenceDate.ShouldBe(new DateOnly(2026, 10, 20));

    [Fact(Skip = "Pending")]
    public async Task SendsTheUtcRunDateWhenReferenceDateIsEmpty()
        => (await CaptureRequestAsync(new ExtractValuesSettings { Context = OrderText, Fields = OrderFields(), ReferenceDate = "" }))
            .ReferenceDate.ShouldBe(new DateOnly(2026, 10, 20));

    #endregion

    #region Scenario: the profile is picked or defaulted

    [Fact(Skip = "Pending")]
    public async Task UsesThePickedProfile()
        => GetPrivateField<Guid?>(await CaptureBuilderAsync(new ExtractValuesSettings
        {
            Context = OrderText,
            Fields = OrderFields(),
            ProfileId = PickedProfileId,
        }), "_profileId").ShouldBe(PickedProfileId);

    [Fact(Skip = "Pending")]
    public async Task LeavesTheProfileUnsetWhenNoneIsPicked()
        // null = the default Extraction profile, resolved by IAIExtractionService.
        => GetPrivateField<Guid?>(await CaptureBuilderAsync(OrderSettings()), "_profileId").ShouldBeNull();

    #endregion

    #region Scenario: found values come out as outputs

    [Fact(Skip = "Pending")]
    public async Task Succeeds()
        => (await RunAsync(OrderSettings())).Status.ShouldBe(ActionResultStatus.Success);

    [Fact(Skip = "Pending")]
    public async Task OutputsTheTextValue()
        => (await OutputAsync()).GetProperty("orderId").GetProperty("value").GetString().ShouldBe("A-88412");

    [Fact(Skip = "Pending")]
    public async Task OutputsFoundTrueForAFoundValue()
        => (await OutputAsync()).GetProperty("orderId").GetProperty("found").GetBoolean().ShouldBeTrue();

    [Fact(Skip = "Pending")]
    public async Task OutputsTheConfidence()
        => (await OutputAsync()).GetProperty("orderId").GetProperty("confidence").GetDouble().ShouldBe(0.9999);

    [Fact(Skip = "Pending")]
    public async Task OutputsTheNumberValue()
        => (await OutputAsync()).GetProperty("amount").GetProperty("value").GetDecimal().ShouldBe(701.10m);

    [Fact(Skip = "Pending")]
    public async Task OutputsTheDateAsAnIsoString()
        => (await OutputAsync()).GetProperty("deliveredOn").GetProperty("value").GetString().ShouldBe("2026-10-03");

    [Fact(Skip = "Pending")]
    public async Task OutputsTheDurationValue()
        => (await OutputAsync()).GetProperty("returnWindow").GetProperty("value").GetDecimal().ShouldBe(14m);

    [Fact(Skip = "Pending")]
    public async Task OutputsTheListValues()
        => (await OutputAsync()).GetProperty("damaged").GetProperty("value").EnumerateArray().Select(v => v.GetString())
            .ShouldBe(["mug", "teapot"]);

    #endregion

    #region Scenario: a field the content doesn't contain still has an output

    [Fact(Skip = "Pending")]
    public async Task OutputsEveryAlias()
        => (await OutputAsync()).EnumerateObject().Select(p => p.Name)
            .ShouldBe(["orderId", "amount", "deliveredOn", "returnWindow", "damaged", "invoiceNo"], ignoreOrder: true);

    [Fact(Skip = "Pending")]
    public async Task OutputsFoundFalseForAMissingValue()
        => (await OutputAsync()).GetProperty("invoiceNo").GetProperty("found").GetBoolean().ShouldBeFalse();

    [Fact(Skip = "Pending")]
    public async Task OutputsANullValueForAMissingValue()
        => (await OutputAsync()).GetProperty("invoiceNo").GetProperty("value").ValueKind.ShouldBe(JsonValueKind.Null);

    [Fact(Skip = "Pending")]
    public async Task OutputsANullConfidenceWhenTheProviderReportsNone()
        => (await OutputAsync()).GetProperty("invoiceNo").GetProperty("confidence").ValueKind.ShouldBe(JsonValueKind.Null);

    #endregion

    #region Scenario: settings round-trip the editor's exact camelCase JSON shape

    private const string EditorShapedJson = """
        {
            "context": "${trigger.body}",
            "referenceDate": "2026-10-20",
            "profileId": "11111111-1111-1111-1111-111111111111",
            "fields": [
                { "kind": "text", "alias": "orderId", "instructions": "The order number" },
                { "kind": "number", "alias": "amount", "instructions": "Amount paid", "decimals": 2, "integer": false, "unit": "DKK" },
                { "kind": "list", "alias": "damaged", "instructions": "Damaged products", "itemType": "text" }
            ]
        }
        """;

    private static readonly JsonSerializerOptions EditorShapedJsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    [Fact(Skip = "Pending")]
    public void SettingsDeserialize_FieldsInTheSavedOrder()
        => JsonSerializer.Deserialize<ExtractValuesSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Fields.Select(f => f.Alias).ShouldBe(["orderId", "amount", "damaged"]);

    [Fact(Skip = "Pending")]
    public void SettingsDeserialize_TheNumberDecimals()
        => JsonSerializer.Deserialize<ExtractValuesSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Fields.Single(f => f.Alias == "amount").Decimals.ShouldBe(2);

    [Fact(Skip = "Pending")]
    public void SettingsDeserialize_TheListItemType()
        => JsonSerializer.Deserialize<ExtractValuesSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .Fields.Single(f => f.Alias == "damaged").ItemType.ShouldBe("text");

    [Fact(Skip = "Pending")]
    public void SettingsDeserialize_TheReferenceDate()
        => JsonSerializer.Deserialize<ExtractValuesSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .ReferenceDate.ShouldBe("2026-10-20");

    [Fact(Skip = "Pending")]
    public void SettingsDeserialize_TheProfilePickersGuid()
        => JsonSerializer.Deserialize<ExtractValuesSettings>(EditorShapedJson, EditorShapedJsonOptions)!
            .ProfileId.ShouldBe(PickedProfileId);

    #endregion

    #region Sad path: flag turned off after startup

    [Fact(Skip = "Pending")]
    public async Task WhenFlagOff_FailsWithValidation()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        var result = await RunAsync(OrderSettings());

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact(Skip = "Pending")]
    public async Task WhenFlagOff_SaysExtractionIsDisabled()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        var result = await RunAsync(OrderSettings());

        result.Exception!.Message.ShouldBe("Extraction is disabled.");
    }

    [Fact(Skip = "Pending")]
    public async Task WhenFlagOff_DoesNotCallTheProvider()
    {
        _experimentalMock.Setup(x => x.IsCapabilityEnabled(It.IsAny<Umbraco.AI.Core.Models.AICapability>())).Returns(false);

        await RunAsync(OrderSettings());

        VerifyNoExtractionCall();
    }

    #endregion

    #region Sad path: invalid settings

    public static TheoryData<string, ExtractValuesSettings> InvalidSettings => new()
    {
        { "empty content", new ExtractValuesSettings { Context = "", Fields = OrderFields() } },
        { "whitespace content", new ExtractValuesSettings { Context = "   ", Fields = OrderFields() } },
        { "a reference date that isn't ISO", new ExtractValuesSettings { Context = OrderText, Fields = OrderFields(), ReferenceDate = "next week" } },
        { "no fields", new ExtractValuesSettings { Context = OrderText, Fields = [] } },
        {
            "more than 20 fields",
            new ExtractValuesSettings
            {
                Context = OrderText,
                Fields = Enumerable.Range(0, 21).Select(i => new ExtractValuesField { Kind = "text", Alias = $"f{i}", Instructions = "?" }).ToList(),
            }
        },
        {
            "a duplicate alias",
            new ExtractValuesSettings
            {
                Context = OrderText,
                Fields =
                [
                    new() { Kind = "text", Alias = "orderId", Instructions = "One" },
                    new() { Kind = "text", Alias = "orderId", Instructions = "Two" },
                ],
            }
        },
        {
            "an alias that starts with a digit and has a hyphen",
            new ExtractValuesSettings { Context = OrderText, Fields = [new() { Kind = "text", Alias = "1st-value", Instructions = "?" }] }
        },
        {
            "an unknown kind",
            new ExtractValuesSettings { Context = OrderText, Fields = [new() { Kind = "estimate", Alias = "guess", Instructions = "?" }] }
        },
    };

    [Theory(Skip = "Pending")]
    [MemberData(nameof(InvalidSettings))]
    public async Task InvalidSettings_FailWithValidation(string _, ExtractValuesSettings settings)
        => (await RunAsync(settings)).ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);

    [Theory(Skip = "Pending")]
    [MemberData(nameof(InvalidSettings))]
    public async Task InvalidSettings_DoNotCallTheProvider(string _, ExtractValuesSettings settings)
    {
        await RunAsync(settings);

        VerifyNoExtractionCall();
    }

    #endregion

    #region Sad path: Core rejects the request (e.g. decimals out of range)

    [Fact(Skip = "Pending")]
    public async Task WhenCoreRejectsTheRequest_FailsWithValidation()
    {
        // Core's ValidatingExtractionClient owns the 0..6 decimals rule and throws ArgumentException
        // before any provider call; the action maps it to Validation, as AskDecisionsAction does.
        SetupServiceThrows(new ArgumentException("Decimals must be between 0 and 6.", "request"));

        var result = await RunAsync(OrderSettings());

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    #region Sad path: no profile resolves

    [Fact(Skip = "Pending")]
    public async Task WhenNoProfileResolves_FailsWithValidation()
    {
        SetupServiceThrows(new InvalidOperationException("No default Extraction profile is set."));

        var result = await RunAsync(OrderSettings());

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    #endregion

    #region Sad path: the provider fails

    [Fact(Skip = "Pending")]
    public async Task WhenProviderRejectsTheApiKey_ReportsANonRetryableAuthenticationError()
    {
        SetupServiceThrows(new AIProviderException(
            new AIProviderErrorInfo(AIProviderErrorCategory.Authentication, "invalid api token", ProviderCode: null, "invalid api token")));

        var result = await RunAsync(OrderSettings());

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Authentication);
    }

    [Fact(Skip = "Pending")]
    public async Task WhenProviderIsRateLimited_ReportsRateLimiting()
    {
        SetupServiceThrows(new AIProviderException(
            new AIProviderErrorInfo(AIProviderErrorCategory.RateLimited, "Capacity temporarily exceeded", ProviderCode: "3040", "3040: Capacity temporarily exceeded")));

        var result = await RunAsync(OrderSettings());

        result.ErrorCategory.ShouldBe(StepRunErrorCategory.RateLimiting);
    }

    [Fact(Skip = "Pending")]
    public async Task WhenProviderFails_SurfacesTheProviderMessage()
    {
        SetupServiceThrows(new AIProviderException(
            new AIProviderErrorInfo(AIProviderErrorCategory.Unknown, "Clairic is down", ProviderCode: null, "Clairic is down")));

        var result = await RunAsync(OrderSettings());

        result.Exception!.Message.ShouldContain("Clairic is down");
    }

    #endregion

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private ExtractValuesAction CreateAction()
        => new(
            _infrastructure,
            _extractionServiceMock.Object,
            _experimentalMock.Object,
            new FixedTimeProvider(RunTime),
            Mock.Of<ILogger<ExtractValuesAction>>());

    private Task<ActionResult> RunAsync(ExtractValuesSettings settings)
        => CreateAction().ExecuteAsync(
            new ActionContext
            {
                AutomationId = Guid.NewGuid(),
                RunId = Guid.NewGuid(),
                StepId = Guid.NewGuid(),
                ActionAlias = UmbracoAIAutomateConstants.ActionTypes.ExtractValues,
                Settings = settings,
            },
            CancellationToken.None);

    private async Task<JsonElement> OutputAsync()
    {
        var result = await RunAsync(OrderSettings());
        return JsonSerializer.SerializeToElement(result.OutputData, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<AIExtractionRequest> CaptureRequestAsync(ExtractValuesSettings settings)
    {
        AIExtractionRequest? sent = null;
        _extractionServiceMock
            .Setup(s => s.GetExtractionResponseAsync(It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIExtractionBuilder>, AIExtractionRequest, CancellationToken>((_, r, _) => sent = r)
            .ReturnsAsync(new AIExtractionResponse { Answers = new Dictionary<string, AIExtractionAnswer>() });

        await RunAsync(settings);

        return sent!;
    }

    private async Task<AIExtractionBuilder> CaptureBuilderAsync(ExtractValuesSettings settings)
    {
        Action<AIExtractionBuilder>? captured = null;
        _extractionServiceMock
            .Setup(s => s.GetExtractionResponseAsync(It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIExtractionBuilder>, AIExtractionRequest, CancellationToken>((b, _, _) => captured = b)
            .ReturnsAsync(new AIExtractionResponse { Answers = new Dictionary<string, AIExtractionAnswer>() });

        await RunAsync(settings);

        var builder = new AIExtractionBuilder();
        captured!(builder);
        return builder;
    }

    private void SetupServiceThrows(Exception exception)
        => _extractionServiceMock
            .Setup(s => s.GetExtractionResponseAsync(It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    private void VerifyNoExtractionCall()
        => _extractionServiceMock.Verify(
            s => s.GetExtractionResponseAsync(It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

    /// <summary>
    /// Through <see cref="IStepType"/>, as Automate's catalogue resolves it — same reason and same
    /// round-tripping resolver as AskDecisionsActionTests.SchemaAsync. Serialized to JSON so the
    /// assertions read plain keywords (<c>type</c>, <c>items</c>) rather than JsonSchema.Net's
    /// keyword API.
    /// </summary>
    private static async Task<JsonElement> SchemaJsonAsync(List<ExtractValuesField> fields)
    {
        IStepType action = new ExtractValuesAction(
            new ActionInfrastructure(CreateRoundTrippingModelResolver()),
            Mock.Of<IAIExtractionService>(),
            Mock.Of<IAIExperimentalFeatures>(),
            TimeProvider.System,
            Mock.Of<ILogger<ExtractValuesAction>>());

        var settings = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            JsonSerializer.Serialize(new ExtractValuesSettings { Fields = fields }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var schema = await action.GetOutputSchemaAsync(settings);
        return JsonSerializer.SerializeToElement(schema);
    }

    private static async Task<JsonElement> AliasSchemaAsync(string kind)
        => (await SchemaJsonAsync([new() { Kind = kind, Alias = "f", Instructions = "Something" }]))
            .GetProperty("properties").GetProperty("f");

    private static IEditableModelResolver CreateRoundTrippingModelResolver()
    {
        var resolver = new Mock<IEditableModelResolver>();
        resolver
            .Setup(r => r.ResolveModel<ExtractValuesSettings>(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<EditableModelSchema?>()))
            .Returns((string _, object? data, EditableModelSchema? _) => data is null
                ? null
                : JsonSerializer.Deserialize<ExtractValuesSettings>(
                    JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                    new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true }));
        return resolver.Object;
    }

    private static FieldAttribute GetField(string propertyName)
        => typeof(ExtractValuesSettings).GetProperty(propertyName)!.GetCustomAttribute<FieldAttribute>()!;

    private static T? GetPrivateField<T>(object instance, string fieldName)
        => (T?)instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance);
}

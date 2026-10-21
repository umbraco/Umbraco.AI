#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-1 — Extract typed values from C# (AC1-AC16, AC33-AC35): one typed answer per field.
//
// ASSUMPTIONS (builder confirms/adjusts, keeping each test's behavior and single assertion):
// - ExtractAsync assigns an internal id when Field.Id is null; the fake answers whatever id it was sent.
// - AIExtractionAnswer.IsFound is computed (Value != null, or Values.Count > 0), not set by the provider.
// - Turning Clairic's strings ("2026-10-03", "14:30") into DateOnly/TimeOnly/DateTime is the provider
//   client's job (EX-2), so the fake hands Core the already-typed value.

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Every spec goes through the real <see cref="AIExtractionService"/> + <see cref="AIExtractionClientFactory"/>
/// pipeline (see <see cref="ExtractionPipelineHarness"/>); only the provider client is faked.
/// </summary>
public class ExtractTypedValuesTests
{
    private const string State =
        "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.";

    /// <summary>Answers the request's only field with <paramref name="answer"/>, keyed by whatever id it carries.</summary>
    private static Func<AIExtractionRequest, AIExtractionResponse> AnswerFirst(
        AIExtractionAnswer answer,
        string? modelId = "@cf/cloudflare/clairic",
        UsageDetails? usage = null)
        => request => new AIExtractionResponse
        {
            Answers = new Dictionary<string, AIExtractionAnswer> { [request.Fields[0].Id!] = answer },
            ModelId = modelId,
            Usage = usage ?? new UsageDetails { InputTokenCount = 612, OutputTokenCount = 0 },
        };

    /// <summary>Extracts <paramref name="field"/> from <see cref="State"/> with the default profile.</summary>
    private static AIExtractionResponse<TAnswer> Extract<TAnswer>(AIExtractionAnswer answer, AIExtractionField<TAnswer> field)
        where TAnswer : AIExtractionAnswer
    {
        var harness = new ExtractionPipelineHarness(new FakeExtractionClient(AnswerFirst(answer)));
        return harness.Service.ExtractAsync(field, State).GetAwaiter().GetResult();
    }

    private static AIExtractionRequest TextNumberDateRequest() => new()
    {
        State = State,
        Fields =
        [
            new AITextExtractionField { Id = "orderId", Instructions = "The order number" },
            new AINumberExtractionField { Id = "amount", Instructions = "Amount paid", Decimals = 2, Unit = "DKK" },
            new AIDateExtractionField { Id = "deliveredOn", Instructions = "Delivery date" },
        ],
    };

    private static readonly AITextExtractionAnswer OrderIdAnswer = new() { Value = "A-88412", Span = new AIExtractionSpan(6, 13) };

    private static readonly AINumberExtractionAnswer AmountAnswer = new() { Value = 701.10m };

    private static readonly AIDateExtractionAnswer DeliveredOnAnswer = new() { Value = new DateOnly(2026, 10, 3) };

    #region Happy path

    public class GivenATextFieldTheProviderFindsAt6To13
    {
        private readonly AIExtractionResponse<AITextExtractionAnswer> _response = Extract(
            new AITextExtractionAnswer { Value = "A-88412", Span = new AIExtractionSpan(6, 13) },
            new AITextExtractionField { Instructions = "The order number" });

        [Fact(Skip = "Pending")]
        public void ReturnsATextAnswer() => _response.Answer.ShouldBeOfType<AITextExtractionAnswer>();

        [Fact(Skip = "Pending")]
        public void ReturnsTheValue() => _response.Answer.Value.ShouldBe("A-88412");

        [Fact(Skip = "Pending")]
        public void ReturnsThePosition() => _response.Answer.Span.ShouldBe(new AIExtractionSpan(6, 13));
    }

    public class GivenANumberFieldTheProviderAnswers701Point10
    {
        [Fact(Skip = "Pending")]
        public void ReturnsTheDecimal()
            => Extract(new AINumberExtractionAnswer { Value = 701.10m }, new AINumberExtractionField { Instructions = "Amount paid" })
                .Answer.Value.ShouldBe(701.10m);
    }

    public class GivenADateFieldTheProviderAnswers3October2026
    {
        [Fact(Skip = "Pending")]
        public void ReturnsADateOnly()
            => Extract(
                    new AIDateExtractionAnswer { Value = new DateOnly(2026, 10, 3) },
                    new AIDateExtractionField { Instructions = "Delivery date" })
                .Answer.Value.ShouldBe(new DateOnly(2026, 10, 3));
    }

    public class GivenATimeFieldTheProviderAnswers1430
    {
        [Fact(Skip = "Pending")]
        public void ReturnsATimeOnly()
            => Extract(
                    new AITimeExtractionAnswer { Value = new TimeOnly(14, 30) },
                    new AITimeExtractionField { Instructions = "Delivery time" })
                .Answer.Value.ShouldBe(new TimeOnly(14, 30));
    }

    public class GivenADateTimeFieldTheProviderAnswers3October2026At1430
    {
        [Fact(Skip = "Pending")]
        public void ReturnsADateTime()
            => Extract(
                    new AIDateTimeExtractionAnswer { Value = new DateTime(2026, 10, 3, 14, 30, 0) },
                    new AIDateTimeExtractionField { Instructions = "When it was delivered" })
                .Answer.Value.ShouldBe(new DateTime(2026, 10, 3, 14, 30, 0));
    }

    public class GivenADurationFieldInDaysTheProviderAnswers14
    {
        private readonly AIExtractionResponse<AIDurationExtractionAnswer> _response = Extract(
            new AIDurationExtractionAnswer { Value = 14m, Unit = AIDurationUnit.Days },
            new AIDurationExtractionField { Instructions = "How long the return window is", Unit = AIDurationUnit.Days });

        [Fact(Skip = "Pending")]
        public void ReturnsTheNumber() => _response.Answer.Value.ShouldBe(14m);

        [Fact(Skip = "Pending")]
        public void ReturnsItInTheRequestedUnit() => _response.Answer.Unit.ShouldBe(AIDurationUnit.Days);
    }

    public class GivenAListFieldTheProviderAnswersMugAndTeapot
    {
        private readonly AIExtractionResponse<AIListExtractionAnswer> _response = Extract(
            new AIListExtractionAnswer
            {
                Values = ["mug", "teapot"],
                Spans = [new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)],
            },
            new AIListExtractionField { Instructions = "Damaged products" });

        [Fact(Skip = "Pending")]
        public void ReturnsTheValuesInOrder() => _response.Answer.Values.ShouldBe(["mug", "teapot"]);

        [Fact(Skip = "Pending")]
        public void ReturnsOneSpanPerValueInTheSameOrder()
            => _response.Answer.Spans.ShouldBe([new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)]);
    }

    public class GivenATextANumberAndADateFieldInOneRequest
    {
        private readonly AIExtractionResponse _response;

        public GivenATextANumberAndADateFieldInOneRequest()
        {
            var harness = new ExtractionPipelineHarness(new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["orderId"] = OrderIdAnswer,
                    ["amount"] = AmountAnswer,
                    ["deliveredOn"] = DeliveredOnAnswer,
                },
            }));
            _response = harness.Service.GetExtractionResponseAsync(TextNumberDateRequest()).GetAwaiter().GetResult();
        }

        [Fact(Skip = "Pending")]
        public void KeysOneAnswerByEachFieldId()
            => _response.Answers.Keys.ShouldBe(["orderId", "amount", "deliveredOn"], ignoreOrder: true);

        [Fact(Skip = "Pending")]
        public void AnswersTheTextFieldWithATextAnswer() => _response.Answers["orderId"].ShouldBeOfType<AITextExtractionAnswer>();

        [Fact(Skip = "Pending")]
        public void AnswersTheNumberFieldWithANumberAnswer() => _response.Answers["amount"].ShouldBeOfType<AINumberExtractionAnswer>();

        [Fact(Skip = "Pending")]
        public void AnswersTheDateFieldWithADateAnswer() => _response.Answers["deliveredOn"].ShouldBeOfType<AIDateExtractionAnswer>();
    }

    public class GivenATextFieldTheProviderReportsAsNotPresent
    {
        private readonly AIExtractionResponse<AITextExtractionAnswer> _response = Extract(
            new AITextExtractionAnswer { Value = null, NullProbability = 0.97 },
            new AITextExtractionField { Instructions = "The tracking number" });

        [Fact(Skip = "Pending")]
        public void ReturnsANullValue() => _response.Answer.Value.ShouldBeNull();

        [Fact(Skip = "Pending")]
        public void FlagsItAsNotFound() => _response.Answer.IsFound.ShouldBeFalse();
    }

    public class GivenAListFieldTheProviderReportsAsNotPresent
    {
        [Fact(Skip = "Pending")]
        public void ReturnsAnEmptyListNotNull()
            => Extract(
                    new AIListExtractionAnswer { Values = [], Spans = [] },
                    new AIListExtractionField { Instructions = "Missing accessories" })
                .Answer.Values.ShouldBeEmpty();
    }

    public class GivenAProviderReportingConfidenceAndNullProbability
    {
        private readonly AIExtractionResponse<AITextExtractionAnswer> _response = Extract(
            new AITextExtractionAnswer { Value = "A-88412", Confidence = 0.99, NullProbability = 0.001 },
            new AITextExtractionField { Instructions = "The order number" });

        [Fact(Skip = "Pending")]
        public void PassesTheConfidenceThrough() => _response.Answer.Confidence.ShouldBe(0.99);

        [Fact(Skip = "Pending")]
        public void PassesTheNullProbabilityThrough() => _response.Answer.NullProbability.ShouldBe(0.001);
    }

    public class GivenAProviderReportingNoConfidence
    {
        [Fact(Skip = "Pending")]
        public void LeavesConfidenceNull()
            => Extract(new AITextExtractionAnswer { Value = "A-88412" }, new AITextExtractionField { Instructions = "The order number" })
                .Answer.Confidence.ShouldBeNull();
    }

    public class GivenAProviderReportingItsModelAndUsage
    {
        private readonly AIExtractionResponse<AITextExtractionAnswer> _response = Extract(
            new AITextExtractionAnswer { Value = "A-88412" },
            new AITextExtractionField { Instructions = "The order number" });

        [Fact(Skip = "Pending")]
        public void CarriesTheModelId() => _response.ModelId.ShouldBe("@cf/cloudflare/clairic");

        [Fact(Skip = "Pending")]
        public void CarriesTheInputTokenCount() => _response.Usage!.InputTokenCount.ShouldBe(612);
    }

    public class GivenARequestWithAReferenceDate
    {
        [Fact(Skip = "Pending")]
        public async Task PassesTheReferenceDateToTheProviderClient()
        {
            var client = new FakeExtractionClient();
            var harness = new ExtractionPipelineHarness(client);

            await harness.Service.GetExtractionResponseAsync(new AIExtractionRequest
            {
                State = State,
                ReferenceDate = new DateOnly(2026, 10, 20),
                Fields = [new AIDateExtractionField { Id = "deliveredOn", Instructions = "Delivery date" }],
            });

            client.ReceivedRequests.Single().Request.ReferenceDate.ShouldBe(new DateOnly(2026, 10, 20));
        }
    }

    #endregion

    #region Sad path

    public class GivenAProviderThatLeavesOutAField
    {
        [Fact(Skip = "Pending")]
        public async Task ThrowsAIProviderException()
        {
            var harness = new ExtractionPipelineHarness(new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer> { ["orderId"] = OrderIdAnswer, ["amount"] = AmountAnswer },
            }));

            var act = () => harness.Service.GetExtractionResponseAsync(TextNumberDateRequest());

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAProviderThatAnswersAFieldThatWasNotAsked
    {
        [Fact(Skip = "Pending")]
        public async Task ThrowsAIProviderException()
        {
            var harness = new ExtractionPipelineHarness(new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["orderId"] = OrderIdAnswer,
                    ["amount"] = AmountAnswer,
                    ["deliveredOn"] = DeliveredOnAnswer,
                    ["extra"] = new AITextExtractionAnswer { Value = "surprise" },
                },
            }));

            var act = () => harness.Service.GetExtractionResponseAsync(TextNumberDateRequest());

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAProviderThatAnswersANumberFieldWithText
    {
        [Fact(Skip = "Pending")]
        public async Task ThrowsAIProviderException()
        {
            var harness = new ExtractionPipelineHarness(new FakeExtractionClient(
                AnswerFirst(new AITextExtractionAnswer { Value = "701,10 kr" })));

            var act = () => harness.Service.ExtractAsync(new AINumberExtractionField { Instructions = "Amount paid" }, State);

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    #endregion
}

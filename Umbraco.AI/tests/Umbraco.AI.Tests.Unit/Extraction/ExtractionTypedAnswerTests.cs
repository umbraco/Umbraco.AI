#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-1 — Extract typed values from C# (AC1-AC14, AC33-AC35): one typed answer per field.
//
// These run the client pipeline Core owns on its own (ValidatingExtractionClient, then
// ExtractionAnswerChecker on the response) with only the provider client faked. The service-level
// behaviour (profile resolution, model/usage, reference date) is covered by ExtractTypedValuesTests.
//
// Core receives already-typed values: turning a provider's "2026-10-03" or "14:30" into a DateOnly or
// TimeOnly is the provider client's job, so the fake hands over the typed value.

using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

public class ExtractionTypedAnswerTests
{
    private const string State =
        "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.";

    /// <summary>Sends <paramref name="request"/> through the validating client and the answer checker.</summary>
    private static async Task<AIExtractionResponse> RunAsync(FakeExtractionClient provider, AIExtractionRequest request)
    {
        var response = await new ValidatingExtractionClient(provider).GetResponseAsync(request);
        ExtractionAnswerChecker.Check(request, response);
        return response;
    }

    /// <summary>Extracts <paramref name="field"/> from <see cref="State"/>, with the provider answering <paramref name="answer"/>.</summary>
    private static TAnswer Extract<TAnswer>(AIExtractionAnswer answer, AIExtractionField<TAnswer> field)
        where TAnswer : AIExtractionAnswer
    {
        var request = new AIExtractionRequest { State = State, Fields = [field.WithId("answer")] };
        var provider = new FakeExtractionClient(_ => new AIExtractionResponse
        {
            Answers = new Dictionary<string, AIExtractionAnswer> { ["answer"] = answer },
        });

        return (TAnswer)RunAsync(provider, request).GetAwaiter().GetResult().Answers["answer"];
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
        private readonly AITextExtractionAnswer _answer = Extract(
            new AITextExtractionAnswer { Value = "A-88412", Span = new AIExtractionSpan(6, 13) },
            new AITextExtractionField { Instructions = "The order number" });

        [Fact]
        public void ReturnsTheValue() => _answer.Value.ShouldBe("A-88412");

        [Fact]
        public void ReturnsThePosition() => _answer.Span.ShouldBe(new AIExtractionSpan(6, 13));
    }

    public class GivenANumberFieldTheProviderAnswers701Point10
    {
        [Fact]
        public void ReturnsTheDecimal()
            => Extract(new AINumberExtractionAnswer { Value = 701.10m }, new AINumberExtractionField { Instructions = "Amount paid" })
                .Value.ShouldBe(701.10m);
    }

    public class GivenADateFieldTheProviderAnswers3October2026
    {
        [Fact]
        public void ReturnsADateOnly()
            => Extract(
                    new AIDateExtractionAnswer { Value = new DateOnly(2026, 10, 3) },
                    new AIDateExtractionField { Instructions = "Delivery date" })
                .Value.ShouldBe(new DateOnly(2026, 10, 3));
    }

    public class GivenATimeFieldTheProviderAnswers1430
    {
        [Fact]
        public void ReturnsATimeOnly()
            => Extract(
                    new AITimeExtractionAnswer { Value = new TimeOnly(14, 30) },
                    new AITimeExtractionField { Instructions = "Delivery time" })
                .Value.ShouldBe(new TimeOnly(14, 30));
    }

    public class GivenADateTimeFieldTheProviderAnswers3October2026At1430
    {
        [Fact]
        public void ReturnsADateTime()
            => Extract(
                    new AIDateTimeExtractionAnswer { Value = new DateTime(2026, 10, 3, 14, 30, 0) },
                    new AIDateTimeExtractionField { Instructions = "When it was delivered" })
                .Value.ShouldBe(new DateTime(2026, 10, 3, 14, 30, 0));
    }

    public class GivenADurationFieldInDaysTheProviderAnswers14
    {
        private readonly AIDurationExtractionAnswer _answer = Extract(
            new AIDurationExtractionAnswer { Value = 14m, Unit = AIDurationUnit.Days },
            new AIDurationExtractionField { Instructions = "How long the return window is", Unit = AIDurationUnit.Days });

        [Fact]
        public void ReturnsTheNumber() => _answer.Value.ShouldBe(14m);

        [Fact]
        public void ReturnsItInTheRequestedUnit() => _answer.Unit.ShouldBe(AIDurationUnit.Days);
    }

    public class GivenAListFieldTheProviderAnswersMugAndTeapot
    {
        private readonly AIListExtractionAnswer _answer = Extract(
            new AIListExtractionAnswer
            {
                Values = ["mug", "teapot"],
                Spans = [new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)],
            },
            new AIListExtractionField { Instructions = "Damaged products" });

        [Fact]
        public void ReturnsTheValuesInOrder() => _answer.Values.ShouldBe(["mug", "teapot"]);

        [Fact]
        public void ReturnsOneSpanPerValueInTheSameOrder()
            => _answer.Spans.ShouldBe([new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)]);
    }

    public class GivenATextANumberAndADateFieldInOneRequest
    {
        private readonly AIExtractionResponse _response = RunAsync(
            new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["orderId"] = OrderIdAnswer,
                    ["amount"] = AmountAnswer,
                    ["deliveredOn"] = DeliveredOnAnswer,
                },
            }),
            TextNumberDateRequest()).GetAwaiter().GetResult();

        [Fact]
        public void KeysOneAnswerByEachFieldId()
            => _response.Answers.Keys.ShouldBe(["orderId", "amount", "deliveredOn"], ignoreOrder: true);

        [Fact]
        public void AnswersTheTextFieldWithATextAnswer() => _response.Answers["orderId"].ShouldBeOfType<AITextExtractionAnswer>();

        [Fact]
        public void AnswersTheNumberFieldWithANumberAnswer() => _response.Answers["amount"].ShouldBeOfType<AINumberExtractionAnswer>();

        [Fact]
        public void AnswersTheDateFieldWithADateAnswer() => _response.Answers["deliveredOn"].ShouldBeOfType<AIDateExtractionAnswer>();
    }

    public class GivenATextFieldTheProviderReportsAsNotPresent
    {
        private readonly AITextExtractionAnswer _answer = Extract(
            new AITextExtractionAnswer { Value = null, NullProbability = 0.97 },
            new AITextExtractionField { Instructions = "The tracking number" });

        [Fact]
        public void ReturnsANullValue() => _answer.Value.ShouldBeNull();

        [Fact]
        public void FlagsItAsNotFound() => _answer.IsFound.ShouldBeFalse();
    }

    public class GivenATextFieldTheProviderFinds
    {
        [Fact]
        public void FlagsItAsFound()
            => Extract(new AITextExtractionAnswer { Value = "A-88412" }, new AITextExtractionField { Instructions = "The order number" })
                .IsFound.ShouldBeTrue();
    }

    public class GivenAListFieldTheProviderReportsAsNotPresent
    {
        private readonly AIListExtractionAnswer _answer = Extract(
            new AIListExtractionAnswer { Values = [], Spans = [] },
            new AIListExtractionField { Instructions = "Missing accessories" });

        [Fact]
        public void ReturnsAnEmptyListNotNull() => _answer.Values.ShouldBeEmpty();

        [Fact]
        public void FlagsItAsNotFound() => _answer.IsFound.ShouldBeFalse();
    }

    public class GivenAProviderReportingConfidenceAndNullProbability
    {
        private readonly AITextExtractionAnswer _answer = Extract(
            new AITextExtractionAnswer { Value = "A-88412", Confidence = 0.99, NullProbability = 0.001 },
            new AITextExtractionField { Instructions = "The order number" });

        [Fact]
        public void PassesTheConfidenceThrough() => _answer.Confidence.ShouldBe(0.99);

        [Fact]
        public void PassesTheNullProbabilityThrough() => _answer.NullProbability.ShouldBe(0.001);
    }

    public class GivenAProviderReportingNoConfidence
    {
        [Fact]
        public void LeavesConfidenceNull()
            => Extract(new AITextExtractionAnswer { Value = "A-88412" }, new AITextExtractionField { Instructions = "The order number" })
                .Confidence.ShouldBeNull();
    }

    #endregion

    #region Sad path

    public class GivenAProviderThatLeavesOutAField
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var provider = new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer> { ["orderId"] = OrderIdAnswer, ["amount"] = AmountAnswer },
            });

            var act = () => RunAsync(provider, TextNumberDateRequest());

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAProviderThatAnswersAFieldThatWasNotAsked
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var provider = new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["orderId"] = OrderIdAnswer,
                    ["amount"] = AmountAnswer,
                    ["deliveredOn"] = DeliveredOnAnswer,
                    ["extra"] = new AITextExtractionAnswer { Value = "surprise" },
                },
            });

            var act = () => RunAsync(provider, TextNumberDateRequest());

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAProviderThatAnswersANumberFieldWithText
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var provider = new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer> { ["answer"] = new AITextExtractionAnswer { Value = "701,10 kr" } },
            });
            var request = new AIExtractionRequest
            {
                State = State,
                Fields = [new AINumberExtractionField { Id = "answer", Instructions = "Amount paid" }],
            };

            var act = () => RunAsync(provider, request);

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    public class GivenAProviderThatReportsAConfidenceAboveOne
    {
        [Fact]
        public async Task ThrowsAIProviderException()
        {
            var provider = new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer>
                {
                    ["answer"] = new AITextExtractionAnswer { Value = "A-88412", Confidence = 1.5 },
                },
            });
            var request = new AIExtractionRequest
            {
                State = State,
                Fields = [new AITextExtractionField { Id = "answer", Instructions = "The order number" }],
            };

            var act = () => RunAsync(provider, request);

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    /// <summary>Whether the pipeline rejects <paramref name="answer"/> to <paramref name="field"/> with an <see cref="AIProviderException"/>.</summary>
    private static async Task RejectsAsync(AIExtractionAnswer answer, AIExtractionField field)
    {
        var provider = new FakeExtractionClient(_ => new AIExtractionResponse
        {
            Answers = new Dictionary<string, AIExtractionAnswer> { ["answer"] = answer },
        });
        var request = new AIExtractionRequest { State = State, Fields = [field.WithId("answer")] };

        await Should.ThrowAsync<AIProviderException>(() => RunAsync(provider, request));
    }

    public class GivenAProviderThatReportsAConfidenceBelowZero
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AITextExtractionAnswer { Value = "A-88412", Confidence = -0.1 },
                new AITextExtractionField { Instructions = "The order number" });
    }

    public class GivenAProviderThatReportsANaNConfidence
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AITextExtractionAnswer { Value = "A-88412", Confidence = double.NaN },
                new AITextExtractionField { Instructions = "The order number" });
    }

    public class GivenAProviderThatReportsANullProbabilityAboveOne
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AITextExtractionAnswer { Value = "A-88412", NullProbability = 1.5 },
                new AITextExtractionField { Instructions = "The order number" });
    }

    public class GivenAProviderThatReportsANegativeNullProbability
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AITextExtractionAnswer { Value = "A-88412", NullProbability = -0.5 },
                new AITextExtractionField { Instructions = "The order number" });
    }

    public class GivenAProviderThatAnswersAListWithNullValues
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AIListExtractionAnswer { Values = null!, Spans = [] },
                new AIListExtractionField { Instructions = "Damaged products" });
    }

    public class GivenAProviderThatAnswersAListWithNullSpans
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AIListExtractionAnswer { Values = ["mug"], Spans = null! },
                new AIListExtractionField { Instructions = "Damaged products" });
    }

    public class GivenAProviderThatAnswersADurationInAnotherUnit
    {
        [Fact]
        public Task ThrowsAIProviderException()
            => RejectsAsync(
                new AIDurationExtractionAnswer { Value = 336m, Unit = AIDurationUnit.Hours },
                new AIDurationExtractionField { Instructions = "How long the return window is", Unit = AIDurationUnit.Days });
    }

    #endregion
}

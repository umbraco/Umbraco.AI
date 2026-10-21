#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-1 — Extract typed values from C# (AC9, AC15, AC16, AC33-AC35): the service-level specs, pending for T3.
//
// The pipeline-level versions of the typed-answer specs (AC1-AC14, AC33-AC35) are active in
// ExtractionTypedAnswerTests, written against the client pipeline Core owns. What stays here needs
// IAIExtractionService and the factory (T3): AC9 (one answer per field through GetExtractionResponseAsync),
// AC15 and AC16 (model, usage and reference date), and AC33-AC35 (the provider-answer checks through the service).
//
// ASSUMPTIONS (builder confirms/adjusts, keeping each test's behavior and single assertion):
// - ExtractAsync assigns an internal id when Field.Id is null; the fake answers whatever id it was sent.

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

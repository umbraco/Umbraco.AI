#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-1 — Extract typed values from C# (AC22-AC29): the request rules checked before any provider call.

using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// <see cref="ValidatingExtractionClient"/> on its own, like <c>ValidatingDecisionClientTests</c>. That the
/// real factory puts it outermost is pinned in <see cref="AIExtractionClientFactoryTests"/>.
/// </summary>
public class ExtractionRequestValidationTests
{
    private static AIExtractionRequest Request(params AIExtractionField[] fields) => new() { State = "text", Fields = fields };

    private static AITextExtractionField Text(string? id) => new() { Id = id, Instructions = "The order number" };

    #region Happy path

    public class GivenAValidOneFieldRequest
    {
        private readonly FakeExtractionClient _inner = new();

        [Fact(Skip = "Pending")]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingExtractionClient(_inner);

            await client.GetResponseAsync(Request(Text("orderId")));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAFieldIdOf64Characters
    {
        private readonly FakeExtractionClient _inner = new();

        [Fact(Skip = "Pending")]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingExtractionClient(_inner);

            await client.GetResponseAsync(Request(Text("a" + new string('b', 63))));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenAFieldIdStartingWithAnUnderscore
    {
        private readonly FakeExtractionClient _inner = new();

        [Fact(Skip = "Pending")]
        public async Task PassesItToTheInnerClient()
        {
            var client = new ValidatingExtractionClient(_inner);

            await client.GetResponseAsync(Request(Text("_order_ID2")));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    public class GivenNumberDecimalsAtTheEdgeOfTheRange
    {
        private readonly FakeExtractionClient _inner = new();

        [Theory(Skip = "Pending")]
        [InlineData(0)]
        [InlineData(6)]
        public async Task PassesItToTheInnerClient(int decimals)
        {
            var client = new ValidatingExtractionClient(_inner);

            await client.GetResponseAsync(Request(new AINumberExtractionField { Id = "amount", Instructions = "Amount paid", Decimals = decimals }));

            _inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    #endregion

    #region Sad path

    public class GivenAnInvalidRequest
    {
        /// <summary>One case per rule in ARCHITECTURE.md decision 2. Also used by <see cref="AIExtractionClientFactoryTests"/>.</summary>
        public static TheoryData<string, AIExtractionRequest> Cases => new()
        {
            { "no fields", new AIExtractionRequest { State = "text", Fields = [] } },
            { "a blank field id", Request(Text(" ")) },
            { "two fields with id orderId", Request(Text("orderId"), Text("orderId")) },
            { "an id that starts with a digit and has a hyphen", Request(Text("1st-value")) },
            { "an id of 65 characters", Request(Text("a" + new string('b', 64))) },
            { "blank instructions", Request(new AITextExtractionField { Id = "orderId", Instructions = " " }) },
            { "blank state", new AIExtractionRequest { State = " ", Fields = [Text("orderId")] } },
            { "number decimals of 7", Request(new AINumberExtractionField { Id = "amount", Instructions = "Amount paid", Decimals = 7 }) },
            { "number decimals of -1", Request(new AINumberExtractionField { Id = "amount", Instructions = "Amount paid", Decimals = -1 }) },
            {
                "duration decimals of 7",
                Request(new AIDurationExtractionField { Id = "window", Instructions = "Return window", Unit = AIDurationUnit.Days, Decimals = 7 })
            },
        };

        [Theory(Skip = "Pending")]
        [MemberData(nameof(Cases))]
        public async Task ThrowsArgumentException(string _, AIExtractionRequest request)
        {
            var client = new ValidatingExtractionClient(new FakeExtractionClient());

            var act = () => client.GetResponseAsync(request);

            await Should.ThrowAsync<ArgumentException>(act);
        }

        [Theory(Skip = "Pending")]
        [MemberData(nameof(Cases))]
        public async Task NeverReachesTheInnerClient(string _, AIExtractionRequest request)
        {
            var inner = new FakeExtractionClient();
            var client = new ValidatingExtractionClient(inner);

            await Record.ExceptionAsync(() => client.GetResponseAsync(request));

            inner.ReceivedRequests.ShouldBeEmpty();
        }
    }

    #endregion
}

#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-1 — Extract typed values from C# (AC30-AC32): validation sits outside error classification and tracking.

using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Locks the wrapping order <see cref="AIExtractionClientFactory.CreateClientAsync"/> builds (provider client →
/// <see cref="AIErrorClassifyingExtractionClient"/> → middleware → <see cref="ScopedProfileExtractionClient"/> →
/// <see cref="ValidatingExtractionClient"/> outermost) through the real factory, via
/// <see cref="ExtractionTrackingAndChecksHarness"/>, the same way <c>AIDecisionClientFactoryTests</c> does.
/// </summary>
public class AIExtractionClientFactoryTests
{
    private static FakeExtractionClient ThrowingClient() => new(_ => throw new InvalidOperationException(
        "The provider client must never be reached for a caller error."));

    #region Happy path

    public class GivenAValidRequest
    {
        [Fact(Skip = "Pending")]
        public async Task ReachesTheProviderClientOnce()
        {
            var inner = new FakeExtractionClient();
            var harness = await ExtractionTrackingAndChecksHarness.CreateAsync(inner);

            await harness.Client.GetResponseAsync(new AIExtractionRequest
            {
                State = "Order A-88412",
                Fields = [new AITextExtractionField { Id = "orderId", Instructions = "The order number" }],
            });

            inner.ReceivedRequests.Count.ShouldBe(1);
        }
    }

    #endregion

    #region Sad path

    public class GivenAnInvalidRequest
    {
        [Theory(Skip = "Pending")]
        [MemberData(nameof(ExtractionRequestValidationTests.GivenAnInvalidRequest.Cases), MemberType = typeof(ExtractionRequestValidationTests.GivenAnInvalidRequest))]
        public async Task NeverCallsTheProviderClient(string _, AIExtractionRequest request)
        {
            var inner = ThrowingClient();
            var harness = await ExtractionTrackingAndChecksHarness.CreateAsync(inner);

            await Record.ExceptionAsync(() => harness.Client.GetResponseAsync(request));

            inner.ReceivedRequests.ShouldBeEmpty();
        }

        [Theory(Skip = "Pending")]
        [MemberData(nameof(ExtractionRequestValidationTests.GivenAnInvalidRequest.Cases), MemberType = typeof(ExtractionRequestValidationTests.GivenAnInvalidRequest))]
        public async Task ThrowsArgumentExceptionNotAIProviderException(string _, AIExtractionRequest request)
        {
            var harness = await ExtractionTrackingAndChecksHarness.CreateAsync(ThrowingClient());

            var act = () => harness.Client.GetResponseAsync(request);

            await Should.ThrowAsync<ArgumentException>(act);
        }

        [Theory(Skip = "Pending")]
        [MemberData(nameof(ExtractionRequestValidationTests.GivenAnInvalidRequest.Cases), MemberType = typeof(ExtractionRequestValidationTests.GivenAnInvalidRequest))]
        public async Task WritesNoUsageRecord(string _, AIExtractionRequest request)
        {
            var harness = await ExtractionTrackingAndChecksHarness.CreateAsync(ThrowingClient());

            await Record.ExceptionAsync(() => harness.Client.GetResponseAsync(request));

            harness.UsageRecordingServiceMock.Verify(
                x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    public class GivenAGenuineProviderFailure
    {
        [Fact(Skip = "Pending")]
        public async Task ThrowsAIProviderException()
        {
            var harness = await ExtractionTrackingAndChecksHarness.CreateAsync(new FakeExtractionClient(
                _ => throw new InvalidOperationException("Simulated provider SDK failure.")));

            var act = () => harness.Client.GetResponseAsync(new AIExtractionRequest
            {
                State = "Order A-88412",
                Fields = [new AITextExtractionField { Id = "orderId", Instructions = "The order number" }],
            });

            await Should.ThrowAsync<AIProviderException>(act);
        }
    }

    #endregion
}

#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-8 — Extraction calls are tracked like other capabilities (AC1-AC3): usage and audit.

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Through the real <see cref="AIExtractionClientFactory"/> with a real tracker in its middleware (see
/// <see cref="ExtractionTrackingAndChecksHarness"/>); only the audit and usage sinks are mocked.
/// </summary>
public class ExtractionUsageAndAuditTests
{
    private static AIExtractionRequest OrderNumberRequest() => new()
    {
        State = "Order A-88412 was delivered on 3 October.",
        Fields = [new AITextExtractionField { Id = "orderId", Instructions = "The order number" }],
    };

    #region Happy path

    public class GivenASuccessfulExtractionUsing612InputTokens
    {
        private readonly ExtractionTrackingAndChecksHarness _harness;

        public GivenASuccessfulExtractionUsing612InputTokens()
        {
            _harness = ExtractionTrackingAndChecksHarness.CreateAsync(new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer> { ["orderId"] = new AITextExtractionAnswer { Value = "A-88412" } },
                ModelId = "@cf/cloudflare/clairic",
                Usage = new UsageDetails { InputTokenCount = 612, OutputTokenCount = 0, TotalTokenCount = 612 },
            })).GetAwaiter().GetResult();

            _harness.Client.GetResponseAsync(OrderNumberRequest()).GetAwaiter().GetResult();
        }

        [Fact]
        public void WritesExactlyOneUsageRecord()
            => _harness.UsageRecordingServiceMock.Verify(
                x => x.QueueRecordUsageAsync(It.IsAny<AIUsageRecord>(), It.IsAny<CancellationToken>()),
                Times.Once);

        [Fact]
        public void RecordsTheUsageUnderExtraction()
            => _harness.UsageRecordFactoryMock.Verify(
                x => x.Create(
                    It.Is<AIUsageRecordContext>(c => c.Capability == AICapability.Extraction),
                    It.IsAny<AIUsageRecordResult>()),
                Times.Once);

        [Fact]
        public void RecordsThe612InputTokens()
            => _harness.UsageRecordFactoryMock.Verify(
                x => x.Create(
                    It.IsAny<AIUsageRecordContext>(),
                    It.Is<AIUsageRecordResult>(r => r.Usage != null && r.Usage.InputTokenCount == 612)),
                Times.Once);

        [Fact]
        public void WritesExactlyOneAuditEntryUnderExtraction()
            => _harness.AuditLogFactoryMock.Verify(
                x => x.Create(
                    It.Is<AIAuditContext>(c => c.Capability == AICapability.Extraction),
                    It.IsAny<IReadOnlyDictionary<string, string>?>(),
                    It.IsAny<Guid?>()),
                Times.Once);

        [Fact]
        public void CompletesTheAuditEntry()
            => _harness.AuditLogServiceMock.Verify(
                x => x.QueueCompleteAuditLogAsync(
                    It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    #endregion

    #region Sad path

    public class GivenAProviderError
    {
        private readonly ExtractionTrackingAndChecksHarness _harness;

        public GivenAProviderError()
        {
            _harness = ExtractionTrackingAndChecksHarness.CreateAsync(new FakeExtractionClient(
                _ => throw new InvalidOperationException("Simulated provider failure."))).GetAwaiter().GetResult();

            Record.ExceptionAsync(() => _harness.Client.GetResponseAsync(OrderNumberRequest())).GetAwaiter().GetResult();
        }

        [Fact]
        public void MarksTheAuditEntryAsFailed()
            => _harness.AuditLogServiceMock.Verify(
                x => x.QueueRecordAuditLogFailureAsync(
                    It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                Times.Once);

        [Fact]
        public void NeverMarksTheAuditEntryAsCompleted()
            => _harness.AuditLogServiceMock.Verify(
                x => x.QueueCompleteAuditLogAsync(
                    It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<AIAuditResponse?>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    public class GivenAProviderAnswerOfTheWrongType
    {
        private readonly ExtractionTrackingAndChecksHarness _harness;

        public GivenAProviderAnswerOfTheWrongType()
        {
            _harness = ExtractionTrackingAndChecksHarness.CreateAsync(new FakeExtractionClient(_ => new AIExtractionResponse
            {
                Answers = new Dictionary<string, AIExtractionAnswer> { ["orderId"] = new AINumberExtractionAnswer { Value = 88412m } },
            })).GetAwaiter().GetResult();

            Record.ExceptionAsync(() => _harness.Client.GetResponseAsync(OrderNumberRequest())).GetAwaiter().GetResult();
        }

        /// <summary>The answer check sits inside tracking, so a bad answer is audited as a failed call.</summary>
        [Fact]
        public void MarksTheAuditEntryAsFailed()
            => _harness.AuditLogServiceMock.Verify(
                x => x.QueueRecordAuditLogFailureAsync(
                    It.IsAny<AIAuditLog>(), It.IsAny<AIAuditPrompt?>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    #endregion
}

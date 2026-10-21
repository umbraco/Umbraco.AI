#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability surface

// EX-8 — Extraction calls are tracked like other capabilities (AC4-AC6): executing and executed notifications.
//
// AIExtractionExecutedNotification keeps Decision's constructor shape (no Response property), so AC6
// checks that it is published once and reports success.

using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Extraction;

/// <summary>
/// Through the real <see cref="AIExtractionService"/> + <see cref="AIExtractionClientFactory"/> pipeline (see
/// <see cref="ExtractionPipelineHarness"/>); the event aggregator stands in for the notification handlers.
/// </summary>
public class ExtractionNotificationTests
{
    private const string State = "Order A-88412 was delivered on 3 October.";

    private static AITextExtractionField OrderNumber() => new() { Instructions = "The order number" };

    #region Happy path

    public class GivenAnExecutingHandler
    {
        private readonly List<string> _order = [];

        public GivenAnExecutingHandler()
        {
            var harness = new ExtractionPipelineHarness(new FakeExtractionClient(request =>
            {
                _order.Add("provider");
                return new AIExtractionResponse
                {
                    Answers = new Dictionary<string, AIExtractionAnswer> { [request.Fields[0].Id!] = new AITextExtractionAnswer { Value = "A-88412" } },
                };
            }));
            harness.EventAggregatorMock
                .Setup(x => x.PublishAsync(It.IsAny<AIExtractionExecutingNotification>(), It.IsAny<CancellationToken>()))
                .Callback(() => _order.Add("executing"))
                .Returns(Task.CompletedTask);

            harness.Service.ExtractAsync(OrderNumber(), State).GetAwaiter().GetResult();
        }

        [Fact]
        public void RunsTheHandlerBeforeTheProviderCall() => _order.ShouldBe(["executing", "provider"]);
    }

    public class GivenAnExecutedHandler
    {
        private readonly ExtractionPipelineHarness _harness;

        public GivenAnExecutedHandler()
        {
            _harness = new ExtractionPipelineHarness(new FakeExtractionClient());
            _ = _harness.Service.ExtractAsync(OrderNumber(), State).GetAwaiter().GetResult();
        }

        [Fact]
        public void PublishesTheExecutedNotificationOnce()
            => _harness.EventAggregatorMock.Verify(
                x => x.PublishAsync(It.IsAny<AIExtractionExecutedNotification>(), It.IsAny<CancellationToken>()),
                Times.Once);

        [Fact]
        public void ReportsTheCallAsSuccessful()
            => _harness.EventAggregatorMock.Verify(
                x => x.PublishAsync(It.Is<AIExtractionExecutedNotification>(n => n.IsSuccess), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    #endregion

    #region Sad path

    public class GivenAnExecutingHandlerThatCancels
    {
        private readonly FakeExtractionClient _client = new();
        private readonly ExtractionPipelineHarness _harness;

        public GivenAnExecutingHandlerThatCancels()
        {
            _harness = new ExtractionPipelineHarness(_client);
            _harness.EventAggregatorMock
                .Setup(x => x.PublishAsync(It.IsAny<AIExtractionExecutingNotification>(), It.IsAny<CancellationToken>()))
                .Callback((AIExtractionExecutingNotification notification, CancellationToken _) => notification.Cancel = true)
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task NeverCallsTheProvider()
        {
            await Record.ExceptionAsync(() => _harness.Service.ExtractAsync(OrderNumber(), State));

            _client.ReceivedRequests.ShouldBeEmpty();
        }

        [Fact]
        public async Task ThrowsInvalidOperationException()
        {
            var act = () => _harness.Service.ExtractAsync(OrderNumber(), State);

            await Should.ThrowAsync<InvalidOperationException>(act);
        }
    }

    #endregion
}

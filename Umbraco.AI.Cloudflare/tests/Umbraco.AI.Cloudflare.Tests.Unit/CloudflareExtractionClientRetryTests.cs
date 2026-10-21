// EX-2 — Connect to Cloudflare (AC22-AC26: retries and error statuses)
#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

using System.Net;
using System.Net.Http.Headers;
using Umbraco.AI.Cloudflare.Tests.Unit.Fakes;
using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Cloudflare.Tests.Unit;

/// <summary>
/// Exercises <see cref="CloudflareExtractionClient"/>'s retry loop through its internal constructor, which
/// takes the backoff delay as a delegate (<see cref="Harness"/> captures each awaited delay instead of
/// sleeping) and a "now" delegate for the HTTP-date form of <c>Retry-After</c>. Copies
/// <c>TypeSafeDecisionClientRetryTests</c>. Requires <c>InternalsVisibleTo("Umbraco.AI.Cloudflare.Tests.Unit")</c>.
/// </summary>
public class CloudflareExtractionClientRetryTests
{
    private const string TextAnswer =
        """{"result":{"answers":{"orderId":{"value":"A-88412","confidence":0.99,"p_null":0.001,"span":[6,13]}},"usage":{"input_tokens":1,"output_tokens":0}},"success":true,"errors":[],"messages":[]}""";

    private const string CapacityExceeded =
        """{"result":null,"success":false,"errors":[{"code":3040,"message":"Capacity temporarily exceeded"}],"messages":[]}""";

    private const string BadInput =
        """{"result":null,"success":false,"errors":[{"code":5006,"message":"Invalid input"}],"messages":[]}""";

    private static readonly AITextExtractionField OrderId = new() { Id = "orderId", Instructions = "The order number" };

    private static Task<AIExtractionResponse> ExtractAsync(CloudflareExtractionClient client)
        => client.GetResponseAsync(new AIExtractionRequest { State = "Order A-88412 was delivered.", Fields = [OrderId] });

    private static Func<HttpResponseMessage> TooManyRequestsRetryAfter(RetryConditionHeaderValue retryAfter)
        => () =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = retryAfter;
            return response;
        };

    private sealed class Harness
    {
        public Harness(params Func<HttpResponseMessage>[] responses)
            : this(now: null, responses)
        {
        }

        public Harness(Func<DateTimeOffset>? now, params Func<HttpResponseMessage>[] responses)
        {
            Handler = new ScriptedHttpMessageHandler(responses);
            Client = new CloudflareExtractionClient(
                new HttpClient(Handler),
                CloudflareTestHost.Settings(),
                CloudflareTestHost.ModelId,
                (delay, _) =>
                {
                    Delays.Add(delay);
                    return Task.CompletedTask;
                },
                now);
        }

        public ScriptedHttpMessageHandler Handler { get; }

        public CloudflareExtractionClient Client { get; }

        public List<TimeSpan> Delays { get; } = [];
    }

    #region Happy path

    public class GivenCloudflareIsBusyOnce
    {
        private readonly Harness _harness = new(
            ScriptedHttpMessageHandler.Status(HttpStatusCode.TooManyRequests),
            ScriptedHttpMessageHandler.Json(TextAnswer));

        [Fact]
        public async Task SucceedsAfterRetrying()
        {
            var response = await ExtractAsync(_harness.Client);

            response.Answers["orderId"].ShouldBeOfType<AITextExtractionAnswer>();
        }

        [Fact]
        public async Task MakesTwoAttempts()
        {
            await ExtractAsync(_harness.Client);

            _harness.Handler.Attempts.ShouldBe(2);
        }
    }

    public class GivenCloudflareAsksToRetryAfterThreeSeconds
    {
        private readonly Harness _harness = new(
            TooManyRequestsRetryAfter(new RetryConditionHeaderValue(TimeSpan.FromSeconds(3))),
            ScriptedHttpMessageHandler.Json(TextAnswer));

        [Fact]
        public async Task WaitsForTheRetryAfterDelay()
        {
            await ExtractAsync(_harness.Client);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(3)]);
        }
    }

    public class GivenCloudflareAsksToRetryAfterAnHttpDateFiveSecondsAhead
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Harness _harness = new(
            () => Now,
            TooManyRequestsRetryAfter(new RetryConditionHeaderValue(Now.AddSeconds(5))),
            ScriptedHttpMessageHandler.Json(TextAnswer));

        [Fact]
        public async Task WaitsForTheTimeUntilTheDate()
        {
            await ExtractAsync(_harness.Client);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(5)]);
        }
    }

    public class GivenCloudflareIsBusyTwiceWithNoRetryAfterHeader
    {
        private readonly Harness _harness = new(
            ScriptedHttpMessageHandler.Status(HttpStatusCode.TooManyRequests),
            ScriptedHttpMessageHandler.Status(HttpStatusCode.TooManyRequests),
            ScriptedHttpMessageHandler.Json(TextAnswer));

        [Fact]
        public async Task BacksOffOneSecondOnTheFirstRetry()
        {
            await ExtractAsync(_harness.Client);

            _harness.Delays[0].ShouldBe(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task BacksOffTwoSecondsOnTheSecondRetry()
        {
            await ExtractAsync(_harness.Client);

            _harness.Delays[1].ShouldBe(TimeSpan.FromSeconds(2));
        }
    }

    #endregion

    #region Sad path

    public class GivenCloudflareAsksToRetryAfter120Seconds
    {
        private readonly Harness _harness = new(
            TooManyRequestsRetryAfter(new RetryConditionHeaderValue(TimeSpan.FromSeconds(120))),
            ScriptedHttpMessageHandler.Json(TextAnswer));

        [Fact]
        public async Task CapsTheDelayAtThirtySeconds()
        {
            await ExtractAsync(_harness.Client);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(30)]);
        }
    }

    public class GivenCloudflareAsksToRetryAfterAnHttpDateFarInTheFuture
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Harness _harness = new(
            () => Now,
            TooManyRequestsRetryAfter(new RetryConditionHeaderValue(Now.AddHours(1))),
            ScriptedHttpMessageHandler.Json(TextAnswer));

        [Fact]
        public async Task CapsTheDelayAtThirtySeconds()
        {
            await ExtractAsync(_harness.Client);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(30)]);
        }
    }

    public class GivenCloudflareIsOverCapacityEveryTime
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Json(CapacityExceeded, HttpStatusCode.TooManyRequests));

        [Fact]
        public async Task FailsAfterExactlyThreeAttempts()
        {
            await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            _harness.Handler.Attempts.ShouldBe(3);
        }

        [Fact]
        public async Task ReportsTheTooManyRequestsStatus()
        {
            var exception = await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            ((HttpRequestException)exception!).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        }

        [Fact]
        public async Task PutsCloudflaresErrorCodeInTheMessage()
        {
            var exception = await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            exception!.Message.ShouldContain("3040");
        }

        [Fact]
        public async Task PutsCloudflaresErrorMessageInTheMessage()
        {
            var exception = await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            exception!.Message.ShouldContain("Capacity temporarily exceeded");
        }
    }

    public class GivenCloudflareRejectsTheRequest
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Json(BadInput, HttpStatusCode.BadRequest));

        [Fact]
        public async Task DoesNotRetry()
        {
            await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            _harness.Handler.Attempts.ShouldBe(1);
        }

        [Fact]
        public async Task ReportsTheBadRequestStatus()
        {
            var exception = await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            ((HttpRequestException)exception!).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task PutsCloudflaresErrorCodeInTheMessage()
        {
            var exception = await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            exception!.Message.ShouldContain("5006");
        }
    }

    public class GivenAnInvalidToken
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status(HttpStatusCode.Unauthorized));

        [Fact]
        public async Task DoesNotRetry()
        {
            await Record.ExceptionAsync(() => ExtractAsync(_harness.Client));

            _harness.Handler.Attempts.ShouldBe(1);
        }
    }

    #endregion
}

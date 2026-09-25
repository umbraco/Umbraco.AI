// DR-2 — Connect to TypeSafe AI (AC11-AC14: retries and error statuses)
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Net;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

/// <summary>
/// Exercises <see cref="TypeSafeDecisionClient"/>'s retry loop through its internal constructor, which takes
/// the backoff delay as a delegate (<see cref="Harness"/> captures each awaited delay instead of sleeping)
/// and, for the HTTP-date form of <c>Retry-After</c>, a "now" delegate so the wait is deterministic instead
/// of racing a real clock. <see cref="TypeSafeDecisionCapability"/> is the only production caller and leaves
/// both delegates at their real defaults (<c>Task.Delay</c> and <see cref="DateTimeOffset.UtcNow"/>).
/// Requires <c>InternalsVisibleTo("Umbraco.AI.TypeSafe.Tests.Unit")</c>.
/// </summary>
public class TypeSafeDecisionClientRetryTests
{
    private const string BinaryAnswer = """{"model":"jev-latest","answers":{"q":{"noul":0.97}},"usage":{"input_tokens":1,"output_tokens":1}}""";

    private static readonly AIBinaryDecisionQuestion Question = new() { Instructions = "Is this spam?" };

    private sealed class Harness
    {
        public Harness(params Func<HttpResponseMessage>[] responses)
            : this(now: null, responses)
        {
        }

        public Harness(Func<DateTimeOffset>? now, params Func<HttpResponseMessage>[] responses)
        {
            Handler = new ScriptedHttpMessageHandler(responses);
            Client = new TypeSafeDecisionClient(
                new HttpClient(Handler),
                new TypeSafeProviderSettings { ApiKey = TypeSafeTestHost.ApiKey, Endpoint = TypeSafeTestHost.Endpoint },
                "jev-latest",
                (delay, _) =>
                {
                    Delays.Add(delay);
                    return Task.CompletedTask;
                },
                now);
        }

        public ScriptedHttpMessageHandler Handler { get; }

        public TypeSafeDecisionClient Client { get; }

        public List<TimeSpan> Delays { get; } = [];
    }

    public class GivenJevIsBusyOnce
    {
        private readonly Harness _harness = new(
            ScriptedHttpMessageHandler.Status(HttpStatusCode.TooManyRequests),
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task SucceedsAfterRetrying()
        {
            var response = await _harness.Client.AskAsync(Question);

            response.ShouldBeOfType<AIBinaryDecisionResponse>();
        }

        [Fact]
        public async Task MakesTwoAttempts()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Handler.Attempts.ShouldBe(2);
        }
    }

    public class GivenJevAsksToRetryAfterThreeSeconds
    {
        private readonly Harness _harness = new(
            () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(3));
                return response;
            },
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task WaitsForTheRetryAfterDelay()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(3)]);
        }
    }

    public class GivenJevAsksToRetryAfter120Seconds
    {
        private readonly Harness _harness = new(
            () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(120));
                return response;
            },
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task CapsTheDelayAtThirtySeconds()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(30)]);
        }
    }

    public class GivenJevAsksToRetryAfterAnHttpDateFiveSecondsAhead
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Harness _harness = new(
            () => Now,
            () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(Now.AddSeconds(5));
                return response;
            },
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task WaitsForTheTimeUntilTheDate()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(5)]);
        }
    }

    public class GivenJevAsksToRetryAfterAnHttpDateInThePast
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Harness _harness = new(
            () => Now,
            () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(Now.AddSeconds(-5));
                return response;
            },
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task RecordsNoDelay()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays.ShouldBe([TimeSpan.Zero]);
        }
    }

    public class GivenJevAsksToRetryAfterAnHttpDateFarInTheFuture
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Harness _harness = new(
            () => Now,
            () =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(Now.AddHours(1));
                return response;
            },
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task CapsTheDelayAtThirtySeconds()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(30)]);
        }
    }

    public class GivenJevIsBusyTwiceWithNoRetryAfterHeader
    {
        private readonly Harness _harness = new(
            ScriptedHttpMessageHandler.Status(HttpStatusCode.TooManyRequests),
            ScriptedHttpMessageHandler.Status(HttpStatusCode.TooManyRequests),
            ScriptedHttpMessageHandler.Json(BinaryAnswer));

        [Fact]
        public async Task BacksOffOneSecondOnTheFirstRetry()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays[0].ShouldBe(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task BacksOffTwoSecondsOnTheSecondRetry()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays[1].ShouldBe(TimeSpan.FromSeconds(2));
        }
    }

    public class GivenJevIsOverloadedEveryTime
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status((HttpStatusCode)529));

        [Fact]
        public async Task FailsAfterExactlyThreeAttempts()
        {
            await Record.ExceptionAsync(() => _harness.Client.AskAsync(Question));

            _harness.Handler.Attempts.ShouldBe(3);
        }

        [Fact]
        public async Task ReportsTheOverloadedStatus()
        {
            var exception = await Should.ThrowAsync<HttpRequestException>(() => _harness.Client.AskAsync(Question));

            exception.StatusCode.ShouldBe((HttpStatusCode)529);
        }
    }

    public class GivenAnInvalidApiKey
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status(HttpStatusCode.Unauthorized));

        [Fact]
        public async Task DoesNotRetry()
        {
            await Record.ExceptionAsync(() => _harness.Client.AskAsync(Question));

            _harness.Handler.Attempts.ShouldBe(1);
        }

        [Fact]
        public async Task ReportsAnAuthenticationFailure()
        {
            // AIErrorClassifyingDecisionClient (not exercised here) maps this HttpRequestException's
            // StatusCode to an auth failure via the provider's ClassifyError.
            var exception = await Should.ThrowAsync<HttpRequestException>(() => _harness.Client.AskAsync(Question));

            exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    public class GivenJevRejectsTheQuestion
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status(HttpStatusCode.UnprocessableEntity));

        [Fact]
        public async Task DoesNotRetry()
        {
            await Record.ExceptionAsync(() => _harness.Client.AskAsync(Question));

            _harness.Handler.Attempts.ShouldBe(1);
        }

        [Fact]
        public async Task ReportsAValidationFailure()
        {
            var exception = await Should.ThrowAsync<HttpRequestException>(() => _harness.Client.AskAsync(Question));

            exception.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }
    }
}

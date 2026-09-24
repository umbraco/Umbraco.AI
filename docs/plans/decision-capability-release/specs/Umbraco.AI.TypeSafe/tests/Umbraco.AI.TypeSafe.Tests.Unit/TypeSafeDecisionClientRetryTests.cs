// DR-2 — Connect to TypeSafe AI (AC11-AC14: retries and error statuses)
#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Net;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

namespace Umbraco.AI.TypeSafe.Tests.Unit;

/// <summary>
/// Builder requirement: <see cref="TypeSafeDecisionClient"/> needs an internal constructor taking the backoff
/// delay as a delegate, so retries are asserted without sleeping:
/// <c>internal TypeSafeDecisionClient(HttpClient httpClient, TypeSafeProviderSettings settings, string? modelId,
/// Func&lt;TimeSpan, CancellationToken, Task&gt; delay)</c>. The public path defaults it to <c>Task.Delay</c>.
/// Requires <c>InternalsVisibleTo("Umbraco.AI.TypeSafe.Tests.Unit")</c>.
/// </summary>
public class TypeSafeDecisionClientRetryTests
{
    private const string BinaryAnswer = """{"model":"jev-latest","answers":{"q":{"noul":0.97}},"usage":{"input_tokens":1,"output_tokens":1}}""";

    private static readonly AIBinaryDecisionQuestion Question = new() { Instructions = "Is this spam?" };

    private sealed class Harness
    {
        public Harness(params Func<HttpResponseMessage>[] responses)
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
                });
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

        [Fact(Skip = "Pending T7")]
        public async Task SucceedsAfterRetrying()
        {
            var response = await _harness.Client.AskAsync(Question);

            response.ShouldBeOfType<AIBinaryDecisionResponse>();
        }

        [Fact(Skip = "Pending T7")]
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

        [Fact(Skip = "Pending T7")]
        public async Task WaitsForTheRetryAfterDelay()
        {
            await _harness.Client.AskAsync(Question);

            _harness.Delays.ShouldBe([TimeSpan.FromSeconds(3)]);
        }
    }

    public class GivenJevIsOverloadedEveryTime
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status((HttpStatusCode)529));

        [Fact(Skip = "Pending T7")]
        public async Task FailsAfterExactlyThreeAttempts()
        {
            await Record.ExceptionAsync(() => _harness.Client.AskAsync(Question));

            _harness.Handler.Attempts.ShouldBe(3);
        }

        [Fact(Skip = "Pending T7")]
        public async Task ReportsTheOverloadedStatus()
        {
            var exception = await Should.ThrowAsync<HttpRequestException>(() => _harness.Client.AskAsync(Question));

            exception.StatusCode.ShouldBe((HttpStatusCode)529);
        }
    }

    public class GivenAnInvalidApiKey
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status(HttpStatusCode.Unauthorized));

        [Fact(Skip = "Pending T7")]
        public async Task DoesNotRetry()
        {
            await Record.ExceptionAsync(() => _harness.Client.AskAsync(Question));

            _harness.Handler.Attempts.ShouldBe(1);
        }

        [Fact(Skip = "Pending T7")]
        public async Task ReportsAnAuthenticationFailure()
        {
            // T7 confirms which exception type AIErrorClassifyingDecisionClient maps to an auth failure;
            // if it's not HttpRequestException-with-status, change the expected type, keep the assertion.
            var exception = await Should.ThrowAsync<HttpRequestException>(() => _harness.Client.AskAsync(Question));

            exception.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }

    public class GivenJevRejectsTheQuestion
    {
        private readonly Harness _harness = new(ScriptedHttpMessageHandler.Status(HttpStatusCode.UnprocessableEntity));

        [Fact(Skip = "Pending T7")]
        public async Task DoesNotRetry()
        {
            await Record.ExceptionAsync(() => _harness.Client.AskAsync(Question));

            _harness.Handler.Attempts.ShouldBe(1);
        }

        [Fact(Skip = "Pending T7")]
        public async Task ReportsAValidationFailure()
        {
            var exception = await Should.ThrowAsync<HttpRequestException>(() => _harness.Client.AskAsync(Question));

            exception.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }
    }
}

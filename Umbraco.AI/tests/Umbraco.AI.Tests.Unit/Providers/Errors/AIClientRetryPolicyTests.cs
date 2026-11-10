using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http;
using System.Text;
using Shouldly;
using Umbraco.AI.Core.Providers.Errors;
using Xunit;

namespace Umbraco.AI.Tests.Unit.Providers.Errors;

public class AIClientRetryPolicyTests
{
    private const string OutOfCreditBody =
        """{"error":{"message":"You exceeded your current quota.","type":"insufficient_quota","code":"insufficient_quota"}}""";

    private const string RateLimitBody =
        """{"error":{"message":"Rate limit reached.","type":"requests","code":"rate_limit_exceeded"}}""";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Send_OutOfCredit_IsNotRetried(bool streamed)
    {
        var handler = new CountingHandler(HttpStatusCode.TooManyRequests, OutOfCreditBody);

        var response = await SendAsync(handler, streamed);

        handler.Calls.ShouldBe(1);
        response.Status.ShouldBe(429);
        response.Content.ToString().ShouldContain("insufficient_quota");
    }

    [Fact]
    public void Send_OutOfCredit_IsNotRetriedOnTheSyncPath()
    {
        var handler = new CountingHandler(HttpStatusCode.TooManyRequests, OutOfCreditBody);
        var (pipeline, message) = CreateMessage(handler, streamed: false);

        pipeline.Send(message);

        handler.Calls.ShouldBe(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, RateLimitBody)]
    [InlineData(HttpStatusCode.TooManyRequests, "")]
    [InlineData(HttpStatusCode.ServiceUnavailable, OutOfCreditBody)]
    public async Task Send_OtherRetryableFailures_AreStillRetried(HttpStatusCode status, string body)
    {
        var handler = new CountingHandler(status, body);

        await SendAsync(handler, streamed: false);

        handler.Calls.ShouldBe(2);
    }

    private static async Task<PipelineResponse> SendAsync(CountingHandler handler, bool streamed)
    {
        var (pipeline, message) = CreateMessage(handler, streamed);
        await pipeline.SendAsync(message);
        return message.Response!;
    }

    private static (ClientPipeline Pipeline, PipelineMessage Message) CreateMessage(CountingHandler handler, bool streamed)
    {
        var pipeline = ClientPipeline.Create(new ClientPipelineOptions
        {
            RetryPolicy = new AIClientRetryPolicy(maxRetries: 1),
            Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
        });

        var message = pipeline.CreateMessage();
        message.Request.Method = "POST";
        message.Request.Uri = new Uri("https://example.test/v1/chat/completions");
        message.BufferResponse = !streamed;
        return (pipeline, message);
    }

    private sealed class CountingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(Send(request, cancellationToken));

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            // Keep the retry immediate so the test doesn't wait out the SDK's backoff.
            response.Headers.Add("Retry-After", "0");
            return response;
        }
    }
}

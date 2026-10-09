using System.Net;

namespace Umbraco.AI.OpenRouter.Tests.Unit.Fakes;

/// <summary>
/// Hands out clients that answer every request with one canned JSON body, so the provider's real
/// <c>GET /models</c> call and deserialization run without touching the network.
/// </summary>
internal sealed class StubHttpClientFactory(string json) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new CannedJsonHandler(json));

    private sealed class CannedJsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
            });
    }
}

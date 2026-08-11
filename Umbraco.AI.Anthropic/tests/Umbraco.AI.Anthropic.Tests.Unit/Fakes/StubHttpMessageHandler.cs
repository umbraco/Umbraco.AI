using System.Net;

namespace Umbraco.AI.Anthropic.Tests.Unit.Fakes;

/// <summary>
/// Serves a queued sequence of canned JSON responses and records the request URIs, so a test can drive the
/// SDK through a multi-page response without a real API call.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<string> _responses;

    public StubHttpMessageHandler(params string[] jsonResponses)
        => _responses = new Queue<string>(jsonResponses);

    public List<string> RequestUris { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestUris.Add(request.RequestUri?.ToString() ?? string.Empty);

        var body = _responses.Count > 0 ? _responses.Dequeue() : """{"data":[],"has_more":false}""";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        });
    }
}

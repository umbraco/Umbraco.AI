using System.Net;

namespace Umbraco.AI.Anthropic.Tests.Unit.Fakes;

/// <summary>
/// Captures the body of every request and short-circuits with an error response, so a test can assert on
/// what would have gone over the wire without a real API call.
/// </summary>
internal sealed class CapturingHttpMessageHandler : HttpMessageHandler
{
    public List<string> RequestBodies { get; } = [];

    /// <summary>The headers of every request, lower-cased names mapped to their joined values.</summary>
    public List<Dictionary<string, string>> RequestHeaders { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestHeaders.Add(request.Headers.ToDictionary(
            h => h.Key.ToLowerInvariant(),
            h => string.Join(",", h.Value)));

        if (request.Content is not null)
        {
            RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"type":"error","error":{"type":"invalid_request_error","message":"captured"}}"""),
        };
    }
}

using System.Net;
using System.Text;

namespace Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

/// <summary>
/// Fakes only the outermost transport. Replies with the scripted responses in order (the last one repeats)
/// and records every request so a test can assert on what went over the wire.
/// </summary>
internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses;
    private Func<HttpResponseMessage> _last;

    public ScriptedHttpMessageHandler(params Func<HttpResponseMessage>[] responses)
    {
        _responses = new Queue<Func<HttpResponseMessage>>(responses);
        _last = responses[^1];
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> RequestBodies { get; } = [];

    public int Attempts => Requests.Count;

    public static Func<HttpResponseMessage> Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => () => new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static Func<HttpResponseMessage> Status(HttpStatusCode status)
        => () => new HttpResponseMessage(status) { Content = new StringContent("""{"error":"scripted"}""", Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        if (_responses.Count > 0)
        {
            _last = _responses.Dequeue();
        }

        return _last();
    }
}

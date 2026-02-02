using Microsoft.AspNetCore.Http;
using Umbraco.AI.AGUI.Events;

namespace Umbraco.AI.AGUI.Streaming;

/// <summary>
/// Abstract base class for AG-UI streaming results.
/// </summary>
public abstract class AGUIStreamResult : IResult
{
    private readonly AGUIStreamOptions _options;
    private readonly AGUIEventSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIStreamResult"/> class.
    /// </summary>
    /// <param name="options">The stream options.</param>
    protected AGUIStreamResult(AGUIStreamOptions? options = null)
    {
        _options = options ?? AGUIStreamOptions.Default;
        _serializer = new AGUIEventSerializer(_options);
    }

    /// <summary>
    /// Gets the stream options.
    /// </summary>
    protected AGUIStreamOptions Options => _options;

    /// <summary>
    /// Gets the event serializer.
    /// </summary>
    protected AGUIEventSerializer Serializer => _serializer;

    /// <summary>
    /// Streams events asynchronously.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of events.</returns>
    protected abstract IAsyncEnumerable<IAGUIEvent> StreamEventsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        var cancellationToken = httpContext.RequestAborted;

        // Set SSE headers
        response.ContentType = _options.ContentType;
        response.Headers.CacheControl = _options.CacheControl;
        response.Headers.Connection = "keep-alive";

        // Disable buffering for real-time streaming
        var bufferingFeature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

        await using var writer = new StreamWriter(response.Body, leaveOpen: true);

        try
        {
            await foreach (var @event in StreamEventsAsync(httpContext, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await _serializer.SerializeAsync(@event, writer, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected, this is expected
        }
    }
}

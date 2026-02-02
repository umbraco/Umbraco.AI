using Microsoft.AspNetCore.Http;
using Umbraco.AI.Agui.Events;

namespace Umbraco.AI.Agui.Streaming;

/// <summary>
/// Abstract base class for AG-UI streaming results.
/// </summary>
public abstract class AguiStreamResult : IResult
{
    private readonly AguiStreamOptions _options;
    private readonly AguiEventSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AguiStreamResult"/> class.
    /// </summary>
    /// <param name="options">The stream options.</param>
    protected AguiStreamResult(AguiStreamOptions? options = null)
    {
        _options = options ?? AguiStreamOptions.Default;
        _serializer = new AguiEventSerializer(_options);
    }

    /// <summary>
    /// Gets the stream options.
    /// </summary>
    protected AguiStreamOptions Options => _options;

    /// <summary>
    /// Gets the event serializer.
    /// </summary>
    protected AguiEventSerializer Serializer => _serializer;

    /// <summary>
    /// Streams events asynchronously.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of events.</returns>
    protected abstract IAsyncEnumerable<IAguiEvent> StreamEventsAsync(
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

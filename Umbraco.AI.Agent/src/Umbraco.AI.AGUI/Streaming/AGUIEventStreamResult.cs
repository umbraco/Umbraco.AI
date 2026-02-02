using Microsoft.AspNetCore.Http;
using Umbraco.AI.AGUI.Events;

namespace Umbraco.AI.AGUI.Streaming;

/// <summary>
/// Concrete AG-UI streaming result that wraps an async enumerable of events.
/// Use this when you already have an <see cref="IAsyncEnumerable{IAGUIEvent}"/> to stream.
/// </summary>
/// <example>
/// Minimal API usage:
/// <code>
/// app.MapPost("/agent", (request) =>
/// {
///     var events = agentService.RunAsync(request);
///     return new AGUIEventStreamResult(events);
/// });
/// </code>
///
/// Controller usage:
/// <code>
/// public async Task StreamChat(ChatRequest request, CancellationToken ct)
/// {
///     var events = agentService.RunAsync(request, ct);
///     await new AGUIEventStreamResult(events).ExecuteAsync(HttpContext);
/// }
/// </code>
/// </example>
public sealed class AGUIEventStreamResult : AGUIStreamResult
{
    private readonly IAsyncEnumerable<IAGUIEvent> _events;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIEventStreamResult"/> class.
    /// </summary>
    /// <param name="events">The async enumerable of events to stream.</param>
    /// <param name="options">Optional stream options.</param>
    public AGUIEventStreamResult(IAsyncEnumerable<IAGUIEvent> events, AGUIStreamOptions? options = null)
        : base(options)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<IAGUIEvent> StreamEventsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) => _events;
}

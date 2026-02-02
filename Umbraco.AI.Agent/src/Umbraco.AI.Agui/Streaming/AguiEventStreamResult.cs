using Microsoft.AspNetCore.Http;
using Umbraco.AI.Agui.Events;

namespace Umbraco.AI.Agui.Streaming;

/// <summary>
/// Concrete AG-UI streaming result that wraps an async enumerable of events.
/// Use this when you already have an <see cref="IAsyncEnumerable{IAguiEvent}"/> to stream.
/// </summary>
/// <example>
/// Minimal API usage:
/// <code>
/// app.MapPost("/agent", (request) =>
/// {
///     var events = agentService.RunAsync(request);
///     return new AguiEventStreamResult(events);
/// });
/// </code>
///
/// Controller usage:
/// <code>
/// public async Task StreamChat(ChatRequest request, CancellationToken ct)
/// {
///     var events = agentService.RunAsync(request, ct);
///     await new AguiEventStreamResult(events).ExecuteAsync(HttpContext);
/// }
/// </code>
/// </example>
public sealed class AguiEventStreamResult : AguiStreamResult
{
    private readonly IAsyncEnumerable<IAguiEvent> _events;

    /// <summary>
    /// Initializes a new instance of the <see cref="AguiEventStreamResult"/> class.
    /// </summary>
    /// <param name="events">The async enumerable of events to stream.</param>
    /// <param name="options">Optional stream options.</param>
    public AguiEventStreamResult(IAsyncEnumerable<IAguiEvent> events, AguiStreamOptions? options = null)
        : base(options)
    {
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    /// <inheritdoc />
    protected override IAsyncEnumerable<IAguiEvent> StreamEventsAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken) => _events;
}

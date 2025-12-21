using System.Text.Json;
using Umbraco.Ai.Agui.Events;

namespace Umbraco.Ai.Agui.Streaming;

/// <summary>
/// Serializer for AG-UI events to SSE format.
/// </summary>
public sealed class AguiEventSerializer
{
    private readonly AguiStreamOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AguiEventSerializer"/> class.
    /// </summary>
    /// <param name="options">The stream options.</param>
    public AguiEventSerializer(AguiStreamOptions? options = null)
    {
        _options = options ?? AguiStreamOptions.Default;
    }

    /// <summary>
    /// Serializes an event to SSE format.
    /// The event type discriminator is included in the JSON via JsonPolymorphic.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <returns>The SSE formatted string.</returns>
    public string Serialize(IAguiEvent @event)
    {
        var data = JsonSerializer.Serialize(@event, @event.GetType(), _options.JsonSerializerOptions);
        return $"data: {data}\n\n";
    }

    /// <summary>
    /// Serializes an event to SSE format asynchronously.
    /// The event type discriminator is included in the JSON via JsonPolymorphic.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SerializeAsync(IAguiEvent @event, TextWriter writer, CancellationToken cancellationToken = default)
    {
        var data = JsonSerializer.Serialize(@event, @event.GetType(), _options.JsonSerializerOptions);
        await writer.WriteAsync($"data: {data}\n\n");
        await writer.FlushAsync(cancellationToken);
    }
}

using System.Text.Json;
using Umbraco.AI.AGUI.Events;

namespace Umbraco.AI.AGUI.Streaming;

/// <summary>
/// Serializer for AG-UI events to SSE format.
/// </summary>
public sealed class AGUIEventSerializer
{
    private readonly AGUIStreamOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIEventSerializer"/> class.
    /// </summary>
    /// <param name="options">The stream options.</param>
    public AGUIEventSerializer(AGUIStreamOptions? options = null)
    {
        _options = options ?? AGUIStreamOptions.Default;
    }

    /// <summary>
    /// Serializes an event to SSE format.
    /// The event type discriminator is included in the JSON via JsonPolymorphic on BaseAGUIEvent.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <returns>The SSE formatted string.</returns>
    public string Serialize(IAGUIEvent @event)
    {
        // Serialize as BaseAGUIEvent to include the type discriminator from [JsonPolymorphic]
        var data = JsonSerializer.Serialize(@event, typeof(BaseAGUIEvent), _options.JsonSerializerOptions);
        return $"data: {data}\n\n";
    }

    /// <summary>
    /// Serializes an event to SSE format asynchronously.
    /// The event type discriminator is included in the JSON via JsonPolymorphic on BaseAGUIEvent.
    /// </summary>
    /// <param name="event">The event to serialize.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SerializeAsync(IAGUIEvent @event, TextWriter writer, CancellationToken cancellationToken = default)
    {
        // Serialize as BaseAGUIEvent to include the type discriminator from [JsonPolymorphic]
        var data = JsonSerializer.Serialize(@event, typeof(BaseAGUIEvent), _options.JsonSerializerOptions);
        await writer.WriteAsync($"data: {data}\n\n");
        await writer.FlushAsync(cancellationToken);
    }
}

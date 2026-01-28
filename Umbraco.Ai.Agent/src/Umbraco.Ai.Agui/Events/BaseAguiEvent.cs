using System.Text.Json.Serialization;
using Umbraco.Ai.Agui.Events.Activity;
using Umbraco.Ai.Agui.Events.Lifecycle;
using Umbraco.Ai.Agui.Events.Messages;
using Umbraco.Ai.Agui.Events.Special;
using Umbraco.Ai.Agui.Events.State;
using Umbraco.Ai.Agui.Events.Tools;

namespace Umbraco.Ai.Agui.Events;

/// <summary>
/// Abstract base record for AG-UI events.
/// Uses JsonPolymorphic for automatic type discrimination in JSON serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RunStartedEvent), AguiConstants.EventTypes.RunStarted)]
[JsonDerivedType(typeof(RunFinishedEvent), AguiConstants.EventTypes.RunFinished)]
[JsonDerivedType(typeof(RunErrorEvent), AguiConstants.EventTypes.RunError)]
[JsonDerivedType(typeof(StepStartedEvent), AguiConstants.EventTypes.StepStarted)]
[JsonDerivedType(typeof(StepFinishedEvent), AguiConstants.EventTypes.StepFinished)]
[JsonDerivedType(typeof(TextMessageStartEvent), AguiConstants.EventTypes.TextMessageStart)]
[JsonDerivedType(typeof(TextMessageContentEvent), AguiConstants.EventTypes.TextMessageContent)]
[JsonDerivedType(typeof(TextMessageEndEvent), AguiConstants.EventTypes.TextMessageEnd)]
[JsonDerivedType(typeof(TextMessageChunkEvent), AguiConstants.EventTypes.TextMessageChunk)]
[JsonDerivedType(typeof(ToolCallStartEvent), AguiConstants.EventTypes.ToolCallStart)]
[JsonDerivedType(typeof(ToolCallArgsEvent), AguiConstants.EventTypes.ToolCallArgs)]
[JsonDerivedType(typeof(ToolCallEndEvent), AguiConstants.EventTypes.ToolCallEnd)]
[JsonDerivedType(typeof(ToolCallResultEvent), AguiConstants.EventTypes.ToolCallResult)]
[JsonDerivedType(typeof(ToolCallChunkEvent), AguiConstants.EventTypes.ToolCallChunk)]
[JsonDerivedType(typeof(StateSnapshotEvent), AguiConstants.EventTypes.StateSnapshot)]
[JsonDerivedType(typeof(StateDeltaEvent), AguiConstants.EventTypes.StateDelta)]
[JsonDerivedType(typeof(MessagesSnapshotEvent), AguiConstants.EventTypes.MessagesSnapshot)]
[JsonDerivedType(typeof(ActivitySnapshotEvent), AguiConstants.EventTypes.ActivitySnapshot)]
[JsonDerivedType(typeof(ActivityDeltaEvent), AguiConstants.EventTypes.ActivityDelta)]
[JsonDerivedType(typeof(CustomEvent), AguiConstants.EventTypes.Custom)]
[JsonDerivedType(typeof(RawEvent), AguiConstants.EventTypes.Raw)]
public abstract record BaseAguiEvent : IAguiEvent
{
    /// <summary>
    /// Gets or sets the timestamp of the event in Unix milliseconds.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; init; }

    /// <summary>
    /// Gets or sets optional raw event data for passthrough scenarios.
    /// </summary>
    [JsonPropertyName("rawEvent")]
    public object? RawEvent { get; init; }
}

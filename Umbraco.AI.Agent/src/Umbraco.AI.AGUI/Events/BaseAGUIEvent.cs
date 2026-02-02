using System.Text.Json.Serialization;
using Umbraco.AI.AGUI.Events.Activity;
using Umbraco.AI.AGUI.Events.Lifecycle;
using Umbraco.AI.AGUI.Events.Messages;
using Umbraco.AI.AGUI.Events.Special;
using Umbraco.AI.AGUI.Events.State;
using Umbraco.AI.AGUI.Events.Tools;

namespace Umbraco.AI.AGUI.Events;

/// <summary>
/// Abstract base record for AG-UI events.
/// Uses JsonPolymorphic for automatic type discrimination in JSON serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RunStartedEvent), AGUIConstants.EventTypes.RunStarted)]
[JsonDerivedType(typeof(RunFinishedEvent), AGUIConstants.EventTypes.RunFinished)]
[JsonDerivedType(typeof(RunErrorEvent), AGUIConstants.EventTypes.RunError)]
[JsonDerivedType(typeof(StepStartedEvent), AGUIConstants.EventTypes.StepStarted)]
[JsonDerivedType(typeof(StepFinishedEvent), AGUIConstants.EventTypes.StepFinished)]
[JsonDerivedType(typeof(TextMessageStartEvent), AGUIConstants.EventTypes.TextMessageStart)]
[JsonDerivedType(typeof(TextMessageContentEvent), AGUIConstants.EventTypes.TextMessageContent)]
[JsonDerivedType(typeof(TextMessageEndEvent), AGUIConstants.EventTypes.TextMessageEnd)]
[JsonDerivedType(typeof(TextMessageChunkEvent), AGUIConstants.EventTypes.TextMessageChunk)]
[JsonDerivedType(typeof(ToolCallStartEvent), AGUIConstants.EventTypes.ToolCallStart)]
[JsonDerivedType(typeof(ToolCallArgsEvent), AGUIConstants.EventTypes.ToolCallArgs)]
[JsonDerivedType(typeof(ToolCallEndEvent), AGUIConstants.EventTypes.ToolCallEnd)]
[JsonDerivedType(typeof(ToolCallResultEvent), AGUIConstants.EventTypes.ToolCallResult)]
[JsonDerivedType(typeof(ToolCallChunkEvent), AGUIConstants.EventTypes.ToolCallChunk)]
[JsonDerivedType(typeof(StateSnapshotEvent), AGUIConstants.EventTypes.StateSnapshot)]
[JsonDerivedType(typeof(StateDeltaEvent), AGUIConstants.EventTypes.StateDelta)]
[JsonDerivedType(typeof(MessagesSnapshotEvent), AGUIConstants.EventTypes.MessagesSnapshot)]
[JsonDerivedType(typeof(ActivitySnapshotEvent), AGUIConstants.EventTypes.ActivitySnapshot)]
[JsonDerivedType(typeof(ActivityDeltaEvent), AGUIConstants.EventTypes.ActivityDelta)]
[JsonDerivedType(typeof(CustomEvent), AGUIConstants.EventTypes.Custom)]
[JsonDerivedType(typeof(RawEvent), AGUIConstants.EventTypes.Raw)]
public abstract record BaseAGUIEvent : IAGUIEvent
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

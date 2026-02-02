namespace Umbraco.Ai.Agui;

/// <summary>
/// Constants for AG-UI protocol.
/// </summary>
public static class AguiConstants
{
    /// <summary>
    /// AG-UI event type strings as defined in the specification.
    /// Uses UPPER_SNAKE_CASE to match the official AG-UI protocol.
    /// </summary>
    public static class EventTypes
    {
        // Lifecycle events
        public const string RunStarted = "RUN_STARTED";
        public const string RunFinished = "RUN_FINISHED";
        public const string RunError = "RUN_ERROR";
        public const string StepStarted = "STEP_STARTED";
        public const string StepFinished = "STEP_FINISHED";

        // Message events
        public const string TextMessageStart = "TEXT_MESSAGE_START";
        public const string TextMessageContent = "TEXT_MESSAGE_CONTENT";
        public const string TextMessageEnd = "TEXT_MESSAGE_END";
        public const string TextMessageChunk = "TEXT_MESSAGE_CHUNK";

        // Tool events
        public const string ToolCallStart = "TOOL_CALL_START";
        public const string ToolCallArgs = "TOOL_CALL_ARGS";
        public const string ToolCallEnd = "TOOL_CALL_END";
        public const string ToolCallResult = "TOOL_CALL_RESULT";
        public const string ToolCallChunk = "TOOL_CALL_CHUNK";

        // State events
        public const string StateSnapshot = "STATE_SNAPSHOT";
        public const string StateDelta = "STATE_DELTA";
        public const string MessagesSnapshot = "MESSAGES_SNAPSHOT";

        // Activity events
        public const string ActivitySnapshot = "ACTIVITY_SNAPSHOT";
        public const string ActivityDelta = "ACTIVITY_DELTA";

        // Special events
        public const string Raw = "RAW";
        public const string Custom = "CUSTOM";
    }

    /// <summary>
    /// AG-UI message role strings as defined in the specification.
    /// </summary>
    public static class MessageRoles
    {
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string System = "system";
        public const string Tool = "tool";
        public const string Developer = "developer";
    }

    /// <summary>
    /// AG-UI run outcome strings.
    /// </summary>
    public static class RunOutcomes
    {
        public const string Success = "success";
        public const string Interrupt = "interrupt";
    }
}

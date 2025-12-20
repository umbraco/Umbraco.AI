/**
 * Chat message in the conversation.
 */
export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "tool";
  content: string;
  toolCalls?: ToolCallInfo[];
  /** Required for tool role messages - the ID of the tool call this is responding to */
  toolCallId?: string;
  timestamp: Date;
}

/**
 * Information about a tool call.
 */
export interface ToolCallInfo {
  id: string;
  name: string;
  arguments: string;
  result?: string;
  status: "pending" | "running" | "completed" | "error";
}

/**
 * Interrupt information for human-in-the-loop interactions.
 */
export interface InterruptInfo {
  id: string;
  type: "approval" | "input" | "choice" | "custom";
  title: string;
  message: string;
  options?: InterruptOption[];
  inputConfig?: {
    placeholder?: string;
    multiline?: boolean;
  };
  metadata?: Record<string, unknown>;
}

/**
 * Option for interrupt choices.
 */
export interface InterruptOption {
  value: string;
  label: string;
  variant?: "positive" | "danger" | "default";
}

/**
 * Agent state for displaying progress and status.
 */
export interface AgentState {
  status: "idle" | "thinking" | "executing" | "awaiting_input";
  currentStep?: string;
  progress?: {
    current: number;
    total: number;
    label?: string;
  };
  custom?: Record<string, unknown>;
}

/**
 * Callbacks for AG-UI client events.
 */
export interface AgentClientCallbacks {
  /** Called when a text delta is received */
  onTextDelta?: (delta: string) => void;
  /** Called when text message is complete */
  onTextEnd?: (content: string) => void;
  /** Called when a tool call starts */
  onToolCallStart?: (info: ToolCallInfo) => void;
  /** Called when tool call arguments are complete */
  onToolCallArgsEnd?: (id: string, args: string) => void;
  /** Called when a tool call completes */
  onToolCallEnd?: (id: string) => void;
  /** Called when the run finishes */
  onRunFinished?: (event: RunFinishedEvent) => void;
  /** Called when a state snapshot is received */
  onStateSnapshot?: (state: AgentState) => void;
  /** Called when a state delta is received */
  onStateDelta?: (delta: Partial<AgentState>) => void;
  /** Called when a messages snapshot is received */
  onMessagesSnapshot?: (messages: ChatMessage[]) => void;
  /** Called on error */
  onError?: (error: Error) => void;
}

/**
 * Event fired when a run finishes.
 */
export interface RunFinishedEvent {
  outcome: "success" | "interrupt" | "error";
  interrupt?: InterruptInfo;
  error?: string;
}

/**
 * AG-UI event types from the protocol.
 */
export enum EventType {
  TEXT_MESSAGE_START = "TEXT_MESSAGE_START",
  TEXT_MESSAGE_CONTENT = "TEXT_MESSAGE_CONTENT",
  TEXT_MESSAGE_END = "TEXT_MESSAGE_END",
  TOOL_CALL_START = "TOOL_CALL_START",
  TOOL_CALL_ARGS = "TOOL_CALL_ARGS",
  TOOL_CALL_END = "TOOL_CALL_END",
  RUN_STARTED = "RUN_STARTED",
  RUN_FINISHED = "RUN_FINISHED",
  RUN_ERROR = "RUN_ERROR",
  STATE_SNAPSHOT = "STATE_SNAPSHOT",
  STATE_DELTA = "STATE_DELTA",
  MESSAGES_SNAPSHOT = "MESSAGES_SNAPSHOT",
}

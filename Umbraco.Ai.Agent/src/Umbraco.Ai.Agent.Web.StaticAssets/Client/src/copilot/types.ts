/**
 * Shared domain types for the Copilot feature.
 * These types are used across core, transport, and UI layers.
 */

export interface UaiCopilotAgentItem {
  id: string;
  name: string;
  alias: string;
}

/**
 * Chat message in the conversation.
 * Extends AG-UI Message with additional UI-specific fields.
 */
export interface UaiChatMessage {
  id: string;
  role: "user" | "assistant" | "tool";
  content: string;
  toolCalls?: UaiToolCallInfo[];
  /** Required for tool role messages - the ID of the tool call this is responding to */
  toolCallId?: string;
  timestamp: Date;
}

/**
 * Tool call status matching AG-UI events.
 */
export type UaiToolCallStatus =
  | "pending"           // TOOL_CALL_START received
  | "streaming"         // TOOL_CALL_ARGS being received
  | "awaiting_approval" // Frontend tool waiting for user approval
  | "executing"         // Frontend tool executing (after TOOL_CALL_END)
  | "completed"         // TOOL_CALL_RESULT received or frontend execution done
  | "error";            // Error occurred

/**
 * Information about a tool call.
 */
export interface UaiToolCallInfo {
  id: string;
  name: string;
  arguments: string;
  /** Parsed arguments for frontend tool execution */
  parsedArgs?: Record<string, unknown>;
  result?: string;
  status: UaiToolCallStatus;
}

/**
 * Interrupt information for human-in-the-loop interactions.
 */
export interface UaiInterruptInfo {
  id: string;
  /** Reason for the interrupt (e.g., "tool_execution" for frontend tools) */
  reason?: string;
  type: "approval" | "input" | "choice" | "custom";
  title: string;
  message: string;
  options?: UaiInterruptOption[];
  inputConfig?: {
    placeholder?: string;
    multiline?: boolean;
  };
  /** AG-UI interrupt payload - contains tool-specific data from server */
  payload?: Record<string, unknown>;
  metadata?: Record<string, unknown>;
}

/**
 * Option for interrupt choices.
 */
export interface UaiInterruptOption {
  value: string;
  label: string;
  variant?: "positive" | "danger" | "default";
}

/**
 * Agent state for displaying progress and status.
 */
export interface UaiAgentState {
  status: "idle" | "thinking" | "executing" | "awaiting_input";
  currentStep?: string;
  progress?: {
    current: number;
    total: number;
    label?: string;
  };
  custom?: Record<string, unknown>;
}

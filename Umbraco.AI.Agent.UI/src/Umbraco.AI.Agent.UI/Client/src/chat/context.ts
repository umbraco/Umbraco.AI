import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbContextMinimal } from "@umbraco-cms/backoffice/context-api";
import type { Observable } from "rxjs";
import type { UaiChatMessage, UaiAgentState, UaiInterruptInfo, UaiAgentItem, UaiInputContent } from "./types/index.js";
import type { PendingApproval } from "./services/hitl.context.js";
import type { UaiToolRendererManager } from "./services/tool-renderer.manager.js";

/**
 * A single starter-prompt chip shown in an empty chat, ready to send as-is.
 *
 * `display` is kept separate from `prompt` so a future entry source (e.g. a clamped preview of a long
 * prompt) can differ from what actually gets sent -- v1 always sets `display = prompt`.
 */
export interface UaiStarterPromptEntry {
    /** The full prompt text sent when the chip is clicked. */
    prompt: string;

    /** The label shown on the chip. Equal to `prompt` in v1. */
    display: string;

    /** The agent this entry pins the conversation to when clicked, via `selectAgent`. */
    agentId?: string;

    /** Shown as a tag on the chip only when the surrounding list spans more than one agent. */
    agentName?: string;
}

/**
 * Shared chat context interface.
 *
 * Both UaiCopilotContext and the future UaiChatContext implement this interface.
 * Shared chat components consume UAI_CHAT_CONTEXT. Each surface provides its own implementation.
 *
 * Extends UmbContextMinimal so it can be used with UmbContextToken.
 */
export interface UaiChatContextApi extends UmbContextMinimal {
    /** Observable list of chat messages in the current conversation. */
    readonly messages$: Observable<UaiChatMessage[]>;

    /** Observable for streaming text content during assistant response. */
    readonly streamingContent$: Observable<string>;

    /** Observable for the current agent execution state. */
    readonly agentState$: Observable<UaiAgentState | undefined>;

    /** Observable indicating whether an agent run is in progress. */
    readonly isRunning$: Observable<boolean>;

    /** Observable for HITL interrupt state. */
    readonly hitlInterrupt$: Observable<UaiInterruptInfo | undefined>;

    /** Observable for pending approval with target message for inline rendering. */
    readonly pendingApproval$: Observable<PendingApproval | undefined>;

    /** Observable list of available agents. */
    readonly agents: Observable<UaiAgentItem[]>;

    /** Observable for the currently selected agent. */
    readonly selectedAgent: Observable<UaiAgentItem | undefined>;

    /** Observable for the agent resolved in auto mode (contains agent info from agent_selected event). */
    readonly resolvedAgent$: Observable<{ agentId: string; agentName: string; agentAlias: string } | undefined>;

    /** Tool renderer manager for manifest/element lookup. */
    readonly toolRendererManager: UaiToolRendererManager;

    /** Send a user message to the agent, optionally with multimodal content parts. */
    sendUserMessage(content: string, contentParts?: UaiInputContent[]): Promise<void>;

    /** Abort the current agent run. */
    abortRun(): void;

    /** Regenerate the answer to the last user message, replacing the response that followed it. */
    regenerateLastMessage(): Promise<void>;

    /** Select an agent by ID. */
    selectAgent(agentId: string | undefined): void;

    /** Respond to a HITL interrupt. */
    respondToHitl(response: string): void;

    /**
     * Optional observable of starter-prompt chips for the current empty state. Undefined for a
     * surface that supplies no starters. `<uai-chat>` only renders the starter-prompts element when
     * both this and {@link sendStarterPrompt} are present, so a surface supplying neither renders
     * today's empty state unchanged.
     */
    starterPrompts$?: Observable<UaiStarterPromptEntry[]>;

    /**
     * Optional handler for clicking a starter-prompt chip: pins the conversation to the entry's agent
     * (if any) via {@link selectAgent}, then sends its prompt via {@link sendUserMessage} -- the same
     * path as typing, so a server-persisted surface's pending-first-message flow stays intact.
     */
    sendStarterPrompt?(entry: UaiStarterPromptEntry): void;
}

/**
 * Context token for consuming the shared chat context.
 * This is the primary context that shared chat UI components should consume.
 */
export const UAI_CHAT_CONTEXT = new UmbContextToken<UaiChatContextApi>("UaiChatContext");

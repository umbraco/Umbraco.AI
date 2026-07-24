import type { UaiChatMessage } from "@umbraco-ai/agent-ui";
import type { MessageResponseModel } from "../api/types.gen.js";

/**
 * Maps persisted messages into the chat UI's `UaiChatMessage` shape for **display only**. This seeds
 * the thread when a conversation opens; it does NOT feed the model — on each turn the server supplies
 * the authoritative history from its durable store (the client transmits only the new turn). Because
 * of that, a text-first projection is safe: we render user/assistant text and skip system prompts and
 * bare tool records (tool-call visualisation is reconstructed live during a run, not from history).
 */
export function toDisplayMessages(messages: readonly MessageResponseModel[]): UaiChatMessage[] {
    return messages
        .filter((m) => m.role === "user" || m.role === "assistant")
        .map((m) => ({
            id: m.id,
            role: m.role as "user" | "assistant",
            content: m.contentText ?? "",
            timestamp: new Date(m.dateCreated),
        }))
        .filter((m) => m.content.trim().length > 0);
}

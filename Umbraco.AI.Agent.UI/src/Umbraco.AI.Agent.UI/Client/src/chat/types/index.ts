/**
 * Shared domain types for chat features.
 * Re-exported from @umbraco-ai/agent for convenience.
 */

export type {
    UaiChatMessage,
    UaiToolCallStatus,
    UaiToolCallInfo,
    UaiInterruptInfo,
    UaiInterruptOption,
    UaiAgentState,
    UaiInputContent,
    UaiTextInputContent,
    UaiImageInputContent,
    UaiAudioInputContent,
    UaiVideoInputContent,
    UaiDocumentInputContent,
    UaiInputContentSource,
    UaiInputContentDataSource,
    UaiInputContentUrlSource,
} from "@umbraco-ai/agent";

export { classifyContentKind } from "@umbraco-ai/agent";

/**
 * Agent item for agent selector in chat surfaces.
 */
export interface UaiAgentItem {
    id: string;
    name: string;
    alias: string;
}

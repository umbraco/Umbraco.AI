export type {
    ConversationResponseModel,
    CreateConversationRequestModel,
    UpdateConversationRequestModel,
} from "../api/types.gen.js";

/** Filter for listing conversations (mirrors the management API query). */
export interface UaiConversationCollectionFilter {
    projectId?: string;
    search?: string;
    includeArchived?: boolean;
    skip?: number;
    take?: number;
}

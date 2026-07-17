import type { UaiKnowledgeSetEntityType } from "./entity.js";

/**
 * Item model for an installed knowledge set (summary for lists).
 *
 * Knowledge sets are code-defined and read-only — installed sets are auto-active and there is nothing
 * to create, edit, or delete. Maps from the API's `KnowledgeSetResponseModel`.
 */
export interface UaiKnowledgeSetItemModel {
    unique: string;
    entityType: UaiKnowledgeSetEntityType;
    name: string;
    description: string | null;
    icon: string | null;
    itemCount: number;
}

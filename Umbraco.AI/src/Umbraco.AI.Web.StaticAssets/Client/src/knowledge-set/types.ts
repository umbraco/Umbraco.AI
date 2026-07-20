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

/**
 * A single knowledge item within a set (name, description and full markdown content).
 *
 * Maps from the API's `KnowledgeSetItemModel`. Content is returned inline so an admin can audit
 * exactly what the LLM can see.
 */
export interface UaiKnowledgeSetContentItemModel {
    name: string;
    description: string | null;
    content: string;
}

/**
 * Detail model for an installed knowledge set, including its items.
 *
 * Read-only — maps from the API's `KnowledgeSetDetailResponseModel`. `unique` is the knowledge set id
 * (used for workspace routing).
 */
export interface UaiKnowledgeSetDetailModel {
    unique: string;
    entityType: UaiKnowledgeSetEntityType;
    name: string;
    description: string | null;
    icon: string | null;
    items: Array<UaiKnowledgeSetContentItemModel>;
}

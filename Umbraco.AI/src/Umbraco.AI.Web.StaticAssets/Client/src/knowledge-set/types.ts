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
 * A single knowledge item within a set — metadata only (`key`, `name`, `description`).
 *
 * Maps from the API's `KnowledgeSetItemModel`. Content is **not** included here; it is fetched lazily
 * per item via {@link UaiKnowledgeSetItemContentModel} only when an admin opens the item modal, matching
 * the async item model (content is materialised on demand, never merely to list items).
 */
export interface UaiKnowledgeSetItemDetailModel {
    key: string;
    name: string;
    description: string | null;
}

/**
 * The markdown content for a single knowledge item, fetched on demand.
 *
 * Maps from the API's `KnowledgeSetItemContentResponseModel` (`GET /v1/knowledge-sets/{id}/item/{key}`).
 * Content ships in the assembly (not secret), so returning it for audit is fine.
 */
export interface UaiKnowledgeSetItemContentModel {
    key: string;
    content: string;
}

/**
 * Detail model for an installed knowledge set, including its items (metadata only).
 *
 * Read-only — maps from the API's `KnowledgeSetDetailResponseModel`. `unique` is the knowledge set id
 * (used for workspace routing and to address items for their per-item content fetch).
 */
export interface UaiKnowledgeSetDetailModel {
    unique: string;
    entityType: UaiKnowledgeSetEntityType;
    name: string;
    description: string | null;
    icon: string | null;
    items: Array<UaiKnowledgeSetItemDetailModel>;
}

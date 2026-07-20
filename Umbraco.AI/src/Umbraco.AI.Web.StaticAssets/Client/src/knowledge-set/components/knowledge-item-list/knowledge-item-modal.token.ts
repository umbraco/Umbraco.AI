import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiKnowledgeSetItemDetailModel } from "../../types.js";

/**
 * Data passed to the read-only knowledge item modal.
 *
 * Carries the owning knowledge set id plus the item metadata so the modal can lazily fetch the item's
 * markdown content from `GET /v1/knowledge-sets/{id}/item/{key}` when it opens.
 */
export interface UaiKnowledgeItemModalData {
    knowledgeSetId: string;
    item: UaiKnowledgeSetItemDetailModel;
}

/**
 * The read-only content modal has nothing to return — it is a viewer, not an editor.
 */
export type UaiKnowledgeItemModalValue = never;

export const UAI_KNOWLEDGE_ITEM_MODAL = new UmbModalToken<UaiKnowledgeItemModalData, UaiKnowledgeItemModalValue>(
    "Uai.Modal.KnowledgeItem",
    {
        modal: {
            type: "sidebar",
            size: "medium",
        },
    },
);

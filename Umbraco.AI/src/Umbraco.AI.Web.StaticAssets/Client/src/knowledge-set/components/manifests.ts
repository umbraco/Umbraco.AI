import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

/**
 * Knowledge Set component manifests.
 *
 * Registers the read-only item content modal (mirroring the Context resource-options modal, inverted
 * from edit to view). The item list element is registered via the barrel export chain.
 */
export const knowledgeSetComponentManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "Uai.Modal.KnowledgeItem",
        name: "Knowledge Item Modal",
        element: () => import("./knowledge-item-list/knowledge-item-modal.element.js"),
    },
];

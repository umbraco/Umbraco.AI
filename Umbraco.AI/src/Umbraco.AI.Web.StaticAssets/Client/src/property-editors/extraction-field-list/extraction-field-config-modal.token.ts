import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiExtractionFieldKind, UaiExtractionFieldListItem } from "./types.js";

export interface UaiExtractionFieldConfigModalData {
    kind: UaiExtractionFieldKind;
    existingField?: UaiExtractionFieldListItem;
    /** Every other field's alias in the list, so the modal can refuse a duplicate. */
    otherAliases: string[];
}

export interface UaiExtractionFieldConfigModalValue {
    field: UaiExtractionFieldListItem;
}

export const UAI_EXTRACTION_FIELD_CONFIG_MODAL = new UmbModalToken<
    UaiExtractionFieldConfigModalData,
    UaiExtractionFieldConfigModalValue
>("Uai.Modal.ExtractionFieldConfigEditor", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});

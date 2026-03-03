import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiTestVariation } from "../../types.js";

export interface UaiVariationConfigEditorModalData {
    existingVariation?: UaiTestVariation;
    testFeatureId: string;
}

export interface UaiVariationConfigEditorModalValue {
    variation: UaiTestVariation;
}

export const UAI_VARIATION_CONFIG_EDITOR_MODAL = new UmbModalToken<
    UaiVariationConfigEditorModalData,
    UaiVariationConfigEditorModalValue
>(
    "Uai.Modal.VariationConfigEditor",
    {
        modal: {
            type: "sidebar",
            size: "medium",
        },
    }
);

import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiTestVariation } from "../../types.js";

export interface UaiTestVariationConfigEditorModalData {
    existingVariation?: UaiTestVariation;
    testFeatureId: string;
}

export interface UaiTestVariationConfigEditorModalValue {
    variation: UaiTestVariation;
}

export const UAI_TEST_VARIATION_CONFIG_EDITOR_MODAL = new UmbModalToken<
    UaiTestVariationConfigEditorModalData,
    UaiTestVariationConfigEditorModalValue
>(
    "Uai.Modal.TestVariationConfigEditor",
    {
        modal: {
            type: "sidebar",
            size: "medium",
        },
    }
);

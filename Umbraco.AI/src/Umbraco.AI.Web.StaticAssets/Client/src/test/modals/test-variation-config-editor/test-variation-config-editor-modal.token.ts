import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiTestVariation } from "../../types.js";

export interface UaiTestVariationConfigEditorModalData {
    existingVariation?: UaiTestVariation;
    testFeatureId: string;
}

/**
 * Submit value of the variation config editor modal. Includes `| undefined` so the
 * modal can be registered via UmbModalRouteRegistrationController (whose onSetup
 * return type requires `value` only when the token's value type is non-undefined).
 * The modal itself only ever reaches `submit()` after building a real value, so
 * consumers only need to handle undefined on the route-setup callback path.
 */
export type UaiTestVariationConfigEditorModalValue =
    | {
          variation: UaiTestVariation;
      }
    | undefined;

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

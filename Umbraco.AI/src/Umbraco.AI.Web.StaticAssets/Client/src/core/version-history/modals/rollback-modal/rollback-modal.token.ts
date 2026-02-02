import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiVersionPropertyChange } from "../../types.js";

/**
 * Data passed to the rollback modal.
 */
export interface UaiRollbackModalData {
    /** The source version being compared (the version to rollback to). */
    fromVersion: number;
    /** The target version being compared (usually the current version). */
    toVersion: number;
    /** The list of property changes between the versions. */
    changes: UaiVersionPropertyChange[];
}

/**
 * Value returned from the rollback modal.
 */
export interface UaiRollbackModalValue {
    /** Whether the user confirmed the rollback. */
    rollback: boolean;
}

/**
 * Modal token for the rollback confirmation modal.
 */
export const UAI_ROLLBACK_MODAL = new UmbModalToken<
    UaiRollbackModalData,
    UaiRollbackModalValue
>("Uai.Modal.Rollback", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});

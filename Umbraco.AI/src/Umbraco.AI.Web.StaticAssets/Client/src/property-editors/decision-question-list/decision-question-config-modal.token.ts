import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiDecisionQuestionKind, UaiDecisionQuestionListItem } from "./types.js";

export interface UaiDecisionQuestionConfigModalData {
    kind: UaiDecisionQuestionKind;
    existingQuestion?: UaiDecisionQuestionListItem;
    /** Every other question's alias in the list, so the modal can refuse a duplicate. */
    otherAliases: string[];
}

export interface UaiDecisionQuestionConfigModalValue {
    question: UaiDecisionQuestionListItem;
}

export const UAI_DECISION_QUESTION_CONFIG_MODAL = new UmbModalToken<
    UaiDecisionQuestionConfigModalData,
    UaiDecisionQuestionConfigModalValue
>("Uai.Modal.DecisionQuestionConfigEditor", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});

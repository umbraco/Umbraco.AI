import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiTestExecutionSummaryModalData {
    executionId: string;
}

export type UaiTestExecutionSummaryModalValue = never;

export const UAI_TEST_EXECUTION_SUMMARY_MODAL = new UmbModalToken<
    UaiTestExecutionSummaryModalData,
    UaiTestExecutionSummaryModalValue
>("Uai.Modal.TestExecutionSummary", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});

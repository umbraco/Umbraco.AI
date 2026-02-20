import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiTestRunDetailModalData {
    runId: string;
}

export type UaiTestRunDetailModalValue = never;

export const UAI_TEST_RUN_DETAIL_MODAL = new UmbModalToken<
    UaiTestRunDetailModalData,
    UaiTestRunDetailModalValue
>("UmbracoAI.Modal.TestRun.Detail", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});

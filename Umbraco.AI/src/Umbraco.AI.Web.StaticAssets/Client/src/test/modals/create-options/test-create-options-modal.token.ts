import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiTestCreateOptionsModalData {
    headline?: string;
}

export interface UaiTestCreateOptionsModalValue {
    testFeatureId: string;
}

export const UAI_TEST_CREATE_OPTIONS_MODAL = new UmbModalToken<
    UaiTestCreateOptionsModalData,
    UaiTestCreateOptionsModalValue
>("UmbracoAI.Modal.Test.CreateOptions", {
    modal: {
        type: "dialog",
        size: "small",
    },
});

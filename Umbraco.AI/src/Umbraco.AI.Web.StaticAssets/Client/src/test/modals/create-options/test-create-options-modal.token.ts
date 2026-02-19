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
>("Uai.Modal.TestCreateOptions", {
    modal: {
        type: "sidebar",
        size: "small",
    },
});

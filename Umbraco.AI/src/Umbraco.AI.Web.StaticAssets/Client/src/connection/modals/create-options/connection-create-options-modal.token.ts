import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiConnectionCreateOptionsModalData {
    headline?: string;
}

export interface UaiConnectionCreateOptionsModalValue {
    providerAlias: string;
}

export const UAI_CONNECTION_CREATE_OPTIONS_MODAL = new UmbModalToken<
    UaiConnectionCreateOptionsModalData,
    UaiConnectionCreateOptionsModalValue
>("UmbracoAI.Modal.Connection.CreateOptions", {
    modal: {
        type: "dialog",
        size: "small",
    },
});

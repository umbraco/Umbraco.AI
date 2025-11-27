import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiProfileCreateOptionsModalData {
    headline?: string;
}

export interface UaiProfileCreateOptionsModalValue {
    capability: string;
    connectionId: string;
}

export const UAI_PROFILE_CREATE_OPTIONS_MODAL = new UmbModalToken<
    UaiProfileCreateOptionsModalData,
    UaiProfileCreateOptionsModalValue
>("UmbracoAi.Modal.Profile.CreateOptions", {
    modal: {
        type: "dialog",
        size: "small",
    },
});

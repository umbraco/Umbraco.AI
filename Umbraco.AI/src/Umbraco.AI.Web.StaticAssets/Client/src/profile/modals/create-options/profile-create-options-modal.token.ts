import { UmbModalToken } from "@umbraco-cms/backoffice/modal";

export interface UaiProfileCreateOptionsModalData {
    headline?: string;
}

export interface UaiProfileCreateOptionsModalValue {
    capability: string;
}

export const UAI_PROFILE_CREATE_OPTIONS_MODAL = new UmbModalToken<
    UaiProfileCreateOptionsModalData,
    UaiProfileCreateOptionsModalValue
>("UmbracoAI.Modal.Profile.CreateOptions", {
    modal: {
        type: "dialog",
        size: "small",
    },
});

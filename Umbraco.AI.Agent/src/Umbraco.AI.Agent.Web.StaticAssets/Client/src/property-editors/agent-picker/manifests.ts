import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.AgentPicker",
    name: "AI Agent Picker Property Editor UI",
    element: () => import("./property-editor-ui-agent-picker.element.js"),
    meta: {
        label: "AI Agent Picker",
        icon: "icon-bot",
        group: "Umbraco AI",
        settings: {
            properties: [
                {
                    alias: "surfaceId",
                    label: "Surface",
                    description: "Only show agents assigned to this surface.",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.TextBox",
                },
            ],
        },
    },
};

export const agentPickerPropertyEditorManifests = [propertyEditorUi];

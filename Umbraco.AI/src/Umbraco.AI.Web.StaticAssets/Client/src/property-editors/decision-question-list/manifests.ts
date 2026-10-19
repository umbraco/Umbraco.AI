import type { ManifestModal } from "@umbraco-cms/backoffice/modal";
import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: "propertyEditorUi",
    alias: "Uai.PropertyEditorUi.DecisionQuestionList",
    name: "AI Decision Question List Property Editor UI",
    element: () => import("./property-editor-ui-decision-question-list.element.js"),
    meta: {
        label: "AI Decision Question List",
        icon: "icon-speed-gauge",
        group: "Umbraco AI",
        keywords: ["ai", "umbraco ai", "decision", "questions", "automate"],
        settings: {
            properties: [
                {
                    alias: "max",
                    label: "Maximum Questions",
                    description: "Maximum number of questions allowed (optional, default 20).",
                    propertyEditorUiAlias: "Umb.PropertyEditorUi.Integer",
                },
            ],
        },
    },
};

const configModal: ManifestModal = {
    type: "modal",
    alias: "Uai.Modal.DecisionQuestionConfigEditor",
    name: "Decision Question Config Editor Modal",
    element: () => import("./decision-question-config-modal.element.js"),
};

export const decisionQuestionListPropertyEditorManifests = [propertyEditorUi, configModal];

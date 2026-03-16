import type { ManifestModal } from "@umbraco-cms/backoffice/modal";

export const guardrailModalManifests: Array<ManifestModal> = [
    {
        type: "modal",
        alias: "Uai.Modal.GuardrailRuleConfigEditor",
        name: "Guardrail Rule Config Editor Modal",
        element: () => import("./rule-config-editor/guardrail-rule-config-editor-modal.element.js"),
    },
];

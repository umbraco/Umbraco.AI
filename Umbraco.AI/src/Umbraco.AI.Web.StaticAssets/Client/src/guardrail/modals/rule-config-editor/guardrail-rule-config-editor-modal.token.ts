import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiGuardrailRuleConfig } from "../../types.js";

export interface UaiGuardrailRuleConfigEditorModalData {
    evaluatorId: string;
    evaluatorName: string;
    existingRule?: UaiGuardrailRuleConfig;
}

export interface UaiGuardrailRuleConfigEditorModalValue {
    rule: UaiGuardrailRuleConfig;
}

export const UAI_GUARDRAIL_RULE_CONFIG_EDITOR_MODAL = new UmbModalToken<
    UaiGuardrailRuleConfigEditorModalData,
    UaiGuardrailRuleConfigEditorModalValue
>("Uai.Modal.GuardrailRuleConfigEditor", {
    modal: {
        type: "sidebar",
        size: "medium",
    },
});

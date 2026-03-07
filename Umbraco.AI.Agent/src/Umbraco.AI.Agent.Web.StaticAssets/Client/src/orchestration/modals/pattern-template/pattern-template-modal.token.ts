import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiOrchestrationGraph } from "../../types.js";

export type OrchestrationPatternTemplate =
    | "blank"
    | "sequential"
    | "concurrent"
    | "handoff"
    | "groupChat"
    | "magentic";

export interface UaiOrchestrationPatternTemplateModalData {
    /** Currently unused; reserved for future filtering. */
}

export interface UaiOrchestrationPatternTemplateModalValue {
    graph: UaiOrchestrationGraph;
    templateName: OrchestrationPatternTemplate;
}

export const UAI_ORCHESTRATION_PATTERN_TEMPLATE_MODAL = new UmbModalToken<
    UaiOrchestrationPatternTemplateModalData,
    UaiOrchestrationPatternTemplateModalValue
>("Uai.Modal.OrchestrationPatternTemplate", {
    modal: { type: "dialog", size: "large" },
});

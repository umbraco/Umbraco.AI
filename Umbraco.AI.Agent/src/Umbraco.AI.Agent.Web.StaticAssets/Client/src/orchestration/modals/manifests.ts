export const orchestrationModalManifests: Array<UmbExtensionManifest> = [
    {
        type: "modal",
        alias: "Uai.Modal.OrchestrationAgentNodeEditor",
        name: "Orchestration Agent Node Editor Modal",
        js: () => import("./agent-node-editor/agent-node-editor-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.OrchestrationFunctionNodeEditor",
        name: "Orchestration Function Node Editor Modal",
        js: () => import("./function-node-editor/function-node-editor-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.OrchestrationRouterNodeEditor",
        name: "Orchestration Router Node Editor Modal",
        js: () => import("./router-node-editor/router-node-editor-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.OrchestrationAggregatorNodeEditor",
        name: "Orchestration Aggregator Node Editor Modal",
        js: () => import("./aggregator-node-editor/aggregator-node-editor-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.OrchestrationManagerNodeEditor",
        name: "Orchestration Manager Node Editor Modal",
        js: () => import("./manager-node-editor/manager-node-editor-modal.element.js"),
    },
    {
        type: "modal",
        alias: "Uai.Modal.OrchestrationPatternTemplate",
        name: "Orchestration Pattern Template Modal",
        js: () => import("./pattern-template/pattern-template-modal.element.js"),
    },
];

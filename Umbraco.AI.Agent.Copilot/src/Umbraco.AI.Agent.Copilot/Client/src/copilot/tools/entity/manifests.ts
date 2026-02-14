import type { ManifestUaiAgentToolRenderer, ManifestUaiAgentFrontendTool } from "@umbraco-ai/agent-ui";

const setValueRendererManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.SetValue",
    name: "Set Value Tool Renderer",
    meta: {
        toolName: "set_value",
        label: "Set Value",
        icon: "icon-edit",
        approval: true,
    },
};

const setValueFrontendManifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.SetValue",
    name: "Set Value Frontend Tool",
    api: () => import("./set-value.api.ts"),
    meta: {
        toolName: "set_value",
        description:
            "Update a value on the currently selected entity (document, media, etc.). " +
            "Changes are staged in the workspace - the user must click Save to persist. " +
            "Only supports TextBox and TextArea properties. " +
            "Use the entity context to see available properties and their current values.",
        parameters: {
            type: "object",
            properties: {
                path: {
                    type: "string",
                    description: "The path to the property to update (e.g., 'title', 'description')",
                },
                value: {
                    type: "string",
                    description: "The new value to set for the property",
                },
                culture: {
                    type: "string",
                    description:
                        "Optional: Culture code for variant content (e.g., 'en-US'). Omit for invariant content.",
                },
                segment: {
                    type: "string",
                    description: "Optional: Segment name for segmented content. Omit for non-segmented content.",
                },
            },
            required: ["path", "value"],
        },
        scope: "entity-write",
    },
};

export const manifests = [setValueRendererManifest, setValueFrontendManifest];

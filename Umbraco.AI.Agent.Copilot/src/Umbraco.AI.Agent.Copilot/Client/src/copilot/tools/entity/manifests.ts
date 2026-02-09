import type { ManifestUaiAgentToolRenderer, ManifestUaiAgentFrontendTool } from "@umbraco-ai/agent-ui";

const setPropertyValueRendererManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.SetPropertyValue",
    name: "Set Property Value Tool Renderer",
    meta: {
        toolName: "set_property_value",
        label: "Set Property Value",
        icon: "icon-edit",
        approval: true,
    },
};

const setPropertyValueFrontendManifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.SetPropertyValue",
    name: "Set Property Value Frontend Tool",
    api: () => import("./set-property-value.api.js"),
    meta: {
        toolName: "set_property_value",
        description:
            "Update a property value on the currently selected entity (document, media, etc.). " +
            "Changes are staged in the workspace - the user must click Save to persist. " +
            "Only supports TextBox and TextArea properties. " +
            "Use the entity context to see available properties and their current values.",
        parameters: {
            type: "object",
            properties: {
                alias: {
                    type: "string",
                    description: "The property alias to update (e.g., 'title', 'description')",
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
            required: ["alias", "value"],
        },
        scope: "entity-write",
    },
};

export const manifests = [setPropertyValueRendererManifest, setPropertyValueFrontendManifest];

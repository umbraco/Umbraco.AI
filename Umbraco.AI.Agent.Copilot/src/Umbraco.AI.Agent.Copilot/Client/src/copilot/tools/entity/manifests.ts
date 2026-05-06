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
            "Stages a replacement value on a property of the currently selected entity. " +
            "The user must click Save to persist. " +
            "Pass a string for TextBox/TextArea; pass a JSON object or array (not stringified) for everything else. " +
            "Notes: " +
            "the value is REPLACED, not merged - to add/remove/reorder items in a collection (block list, block grid, multi-node tree picker, multi-url picker, multi-image picker, tags), read the current value (Entity Context or get_umbraco_content) and send the full updated array; " +
            "for any non-string property, fetch the JSON Schema from get_content_type_schema first and match it - the Entity Context only shows formatted values, not the input shape.",
        parameters: {
            type: "object",
            properties: {
                path: {
                    type: "string",
                    description: "The path to the property to update (e.g., 'title', 'description', 'mainContent')",
                },
                value: {
                    description:
                        "The new value to set. Use the JSON Schema from get_content_type_schema or " +
                        "get_property_value_schema to determine the exact shape. May be a string, number, " +
                        "boolean, object, or array depending on the property editor.",
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
        isDestructive: true,
    },
};

export const manifests = [setValueRendererManifest, setValueFrontendManifest];

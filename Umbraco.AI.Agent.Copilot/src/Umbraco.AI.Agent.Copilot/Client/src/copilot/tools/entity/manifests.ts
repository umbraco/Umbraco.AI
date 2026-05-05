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
            "REQUIRED WORKFLOW: Unless the target property is a plain text editor (Umbraco.TextBox, Umbraco.TextArea), " +
            "you MUST first call get_content_type_schema for the entity (or get_property_value_schema for a single data type) " +
            "and construct the value to match the returned JSON Schema exactly. " +
            "Skipping this step for media pickers, block lists, block grids, multi-node tree pickers, multi-url pickers, " +
            "image croppers, sliders, color pickers, rich text or any non-string editor will produce malformed values " +
            "that Umbraco rejects when the user saves. " +
            "Do NOT guess the value shape from the property values shown in the Entity Context system prompt - those are " +
            "human-readable formatted values, NOT the input shape. The same property may be displayed as `[]`, `null`, or " +
            "a summarised string while actually accepting a complex object/array on write. " +
            "" +
            "Updates a value on the currently selected entity (document, media, etc.). " +
            "Changes are staged in the workspace - the user must click Save to persist. " +
            "For Umbraco.TextBox / Umbraco.TextArea pass a plain string. " +
            "For all other editors pass the JSON value as a real object or array (do NOT stringify it).",
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

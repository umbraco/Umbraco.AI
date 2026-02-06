import type { ManifestUaiAgentTool } from "../uai-agent-tool.extension.js";

/**
 * Tool: setPropertyValue
 *
 * Updates a property value on the currently selected entity.
 * Changes are staged in the workspace - user must click Save to persist.
 */
const setPropertyValueManifest: ManifestUaiAgentTool = {
    type: "uaiAgentTool",
    kind: "default",
    alias: "Uai.AgentTool.SetPropertyValue",
    name: "Set Property Value Tool",
    api: () => import("./set-property-value.api.js"),
    meta: {
        toolName: "set_property_value",
        label: "Set Property Value",
        description:
            "Update a property value on the currently selected entity (document, media, etc.). " +
            "Changes are staged in the workspace - the user must click Save to persist. " +
            "Only supports TextBox and TextArea properties. " +
            "Use the entity context to see available properties and their current values.",
        icon: "icon-edit",
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
        approval: true,
    },
};

export const manifests = [setPropertyValueManifest];

import type { ManifestUaiAgentToolRenderer, ManifestUaiAgentFrontendTool } from "@umbraco-ai/agent-ui";

const PATH_DESCRIPTION =
    "Path identifying the leaf to operate on. An array alternating property aliases (strings) and " +
    "block selectors (objects of shape { blockKey: \"<guid>\" }). Even-index entries are property " +
    "aliases; odd-index entries are block selectors. Examples: " +
    "['contentBlocks'] (the contentBlocks property at the root), " +
    "['contentBlocks', { blockKey: 'X' }, 'innerBlocks'] (the innerBlocks property of the block 'X' " +
    "inside contentBlocks).";

const VARIANT_PARAMS = {
    culture: {
        type: "string",
        description: "Optional: Culture code for variant content (e.g., 'en-US'). Omit for invariant content.",
    },
    segment: {
        type: "string",
        description: "Optional: Segment name for segmented content. Omit for non-segmented content.",
    },
} as const;

// ─── set_value ─────────────────────────────────────────────────────────────────

const setValueRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.SetValue",
    name: "Set Value Tool Renderer",
    meta: { toolName: "set_value", label: "Set Value", icon: "icon-edit" },
};

const setValueTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.SetValue",
    name: "Set Value Frontend Tool",
    api: () => import("./set-value.api.ts"),
    meta: {
        toolName: "set_value",
        description:
            "Set the entire value of a property on the selected entity (or a nested property via path). " +
            "Changes are staged in the workspace - the user must click Save to persist. " +
            "Best for simple scalar editors (TextBox, TextArea, Numeric, Toggle, DatePicker). " +
            "For block-list / block-grid / picker collections, use add_item / remove_item / move_item — " +
            "set_value cannot construct envelopes safely. " +
            "Use the entity context to see available properties and their current values.",
        parameters: {
            type: "object",
            properties: {
                path: {
                    type: "array",
                    description: PATH_DESCRIPTION,
                    items: {},
                },
                value: {
                    description:
                        "The new value to set. Use the JSON Schema from get_property_value_schema for " +
                        "complex editors. May be a string, number, boolean, object, or array.",
                },
                ...VARIANT_PARAMS,
            },
            required: ["path", "value"],
        },
        scope: "entity-write",
        isDestructive: true,
    },
};

// ─── add_item ──────────────────────────────────────────────────────────────────

const addItemRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.AddItem",
    name: "Add Item Tool Renderer",
    meta: { toolName: "add_item", label: "Add Item", icon: "icon-add" },
};

const addItemTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.AddItem",
    name: "Add Item Frontend Tool",
    api: () => import("./add-item.api.ts"),
    meta: {
        toolName: "add_item",
        description:
            "Add a new item to a collection-shaped property value (block list, block grid, multi-url " +
            "picker, media picker). Returns the new item's blockKey so the agent can reference it in " +
            "subsequent calls. Changes are staged in the workspace - the user must click Save to persist. " +
            "Element type defaults are filled in server-side; the agent only supplies the values it wants " +
            "set explicitly. For block-list/grid, supply 'elementType' as alias or GUID.",
        parameters: {
            type: "object",
            properties: {
                path: {
                    type: "array",
                    description: PATH_DESCRIPTION,
                    items: {},
                },
                elementType: {
                    type: "string",
                    description:
                        "Element type alias OR GUID for the new item. Required for block-list/grid; " +
                        "ignored for editors with a single shape (most pickers).",
                },
                values: {
                    type: "object",
                    description:
                        "Initial values for the item, keyed by property alias. Properties not supplied " +
                        "are filled in from element-type defaults server-side.",
                },
                settingsValues: {
                    type: "object",
                    description: "Optional initial settings values for editors that support a settings element.",
                },
                position: {
                    type: "integer",
                    minimum: 0,
                    description: "Insertion index in the collection. Omit to append.",
                },
                ...VARIANT_PARAMS,
            },
            required: ["path"],
        },
        scope: "entity-write",
        isDestructive: true,
    },
};

// ─── remove_item ───────────────────────────────────────────────────────────────

const removeItemRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.RemoveItem",
    name: "Remove Item Tool Renderer",
    meta: { toolName: "remove_item", label: "Remove Item", icon: "icon-trash" },
};

const removeItemTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.RemoveItem",
    name: "Remove Item Frontend Tool",
    api: () => import("./remove-item.api.ts"),
    meta: {
        toolName: "remove_item",
        description:
            "Remove the item with the given blockKey from a collection-shaped property value. The agent " +
            "must read the current value (via the entity context) to learn the blockKey first.",
        parameters: {
            type: "object",
            properties: {
                path: { type: "array", description: PATH_DESCRIPTION, items: {} },
                blockKey: {
                    type: "string",
                    format: "uuid",
                    description: "The contentKey of the item to remove (read from the current value).",
                },
                ...VARIANT_PARAMS,
            },
            required: ["path", "blockKey"],
        },
        scope: "entity-write",
        isDestructive: true,
    },
};

// ─── move_item ─────────────────────────────────────────────────────────────────

const moveItemRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.MoveItem",
    name: "Move Item Tool Renderer",
    meta: { toolName: "move_item", label: "Move Item", icon: "icon-navigation" },
};

const moveItemTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.MoveItem",
    name: "Move Item Frontend Tool",
    api: () => import("./move-item.api.ts"),
    meta: {
        toolName: "move_item",
        description:
            "Reorder the item with the given blockKey to a new zero-based position in a collection-shaped " +
            "property value.",
        parameters: {
            type: "object",
            properties: {
                path: { type: "array", description: PATH_DESCRIPTION, items: {} },
                blockKey: {
                    type: "string",
                    format: "uuid",
                    description: "The contentKey of the item to move.",
                },
                position: {
                    type: "integer",
                    minimum: 0,
                    description: "Zero-based target position in the resulting collection.",
                },
                ...VARIANT_PARAMS,
            },
            required: ["path", "blockKey", "position"],
        },
        scope: "entity-write",
        isDestructive: true,
    },
};

// ─── clear_value ───────────────────────────────────────────────────────────────

const clearValueRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.ClearValue",
    name: "Clear Value Tool Renderer",
    meta: { toolName: "clear_value", label: "Clear Value", icon: "icon-delete" },
};

const clearValueTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.ClearValue",
    name: "Clear Value Frontend Tool",
    api: () => import("./clear-value.api.ts"),
    meta: {
        toolName: "clear_value",
        description:
            "Clear the value of the property at the path (sets to the editor's empty representation). " +
            "For block properties this removes all blocks. To remove a single block, use remove_item.",
        parameters: {
            type: "object",
            properties: {
                path: { type: "array", description: PATH_DESCRIPTION, items: {} },
                ...VARIANT_PARAMS,
            },
            required: ["path"],
        },
        scope: "entity-write",
        isDestructive: true,
    },
};

// ─── save ──────────────────────────────────────────────────────────────────────

const saveRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.Save",
    name: "Save Tool Renderer",
    meta: { toolName: "save", label: "Save", icon: "icon-save" },
};

const saveTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.Save",
    name: "Save Frontend Tool",
    api: () => import("./save.api.ts"),
    meta: {
        toolName: "save",
        description:
            "Persist the selected entity's staged changes (the workspace's Save action). " +
            "Use this after add_item / set_value / etc. to commit changes the user has approved. " +
            "Documents stay as drafts; media is saved (media is always live). " +
            "Block workspaces cannot be saved directly — their changes save with the parent document. " +
            "If you need the changes to go live on the public site, use save_and_publish instead.",
        parameters: {
            type: "object",
            properties: {},
        },
        scope: "entity-save",
        isDestructive: true,
        approval: true,
    },
};

// ─── save_and_publish ──────────────────────────────────────────────────────────

const saveAndPublishRenderer: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.SaveAndPublish",
    name: "Save and Publish Tool Renderer",
    meta: { toolName: "save_and_publish", label: "Save and Publish", icon: "icon-globe" },
};

const saveAndPublishTool: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.SaveAndPublish",
    name: "Save and Publish Frontend Tool",
    api: () => import("./save-and-publish.api.ts"),
    meta: {
        toolName: "save_and_publish",
        description:
            "Persist the selected document's staged changes AND publish them to the public site. " +
            "Only documents support publish; media is always live (use save) and block workspaces save " +
            "with their parent document. On multi-variant documents the workspace may surface a " +
            "variant-picker modal — this is the same flow a human gets and cannot be bypassed.",
        parameters: {
            type: "object",
            properties: {},
        },
        scope: "entity-publish",
        isDestructive: true,
        approval: true,
    },
};

export const manifests = [
    setValueRenderer,
    setValueTool,
    addItemRenderer,
    addItemTool,
    removeItemRenderer,
    removeItemTool,
    moveItemRenderer,
    moveItemTool,
    clearValueRenderer,
    clearValueTool,
    saveRenderer,
    saveTool,
    saveAndPublishRenderer,
    saveAndPublishTool,
];

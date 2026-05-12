import type { ManifestUaiPropertyValuePreparer } from "./extension-type.js";
import { UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE } from "./extension-type.js";

export const valuePreparerManifests: ManifestUaiPropertyValuePreparer[] = [
    {
        type: UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
        alias: "UmbracoAI.ValuePreparer.BlockList",
        name: "Block List Value Preparer",
        forPropertyEditorSchemaAlias: "Umbraco.BlockList",
        api: () => import("./block-envelope.preparer.js"),
    },
    {
        type: UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
        alias: "UmbracoAI.ValuePreparer.BlockGrid",
        name: "Block Grid Value Preparer",
        forPropertyEditorSchemaAlias: "Umbraco.BlockGrid",
        api: () => import("./block-envelope.preparer.js"),
    },
    {
        type: UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
        alias: "UmbracoAI.ValuePreparer.RichText",
        name: "Rich Text Value Preparer",
        forPropertyEditorSchemaAlias: "Umbraco.RichText",
        api: () => import("./rich-text.preparer.js"),
    },
    {
        type: UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
        alias: "UmbracoAI.ValuePreparer.MediaPicker3",
        name: "Media Picker 3 Value Preparer",
        forPropertyEditorSchemaAlias: "Umbraco.MediaPicker3",
        api: () => import("./media-picker-3.preparer.js"),
    },
    {
        type: UAI_PROPERTY_VALUE_PREPARER_EXTENSION_TYPE,
        alias: "UmbracoAI.ValuePreparer.DateTime",
        name: "Date Time Value Preparer",
        forPropertyEditorSchemaAlias: "Umbraco.DateTime",
        api: () => import("./date-time.preparer.js"),
    },
];

import { contextPickerPropertyEditorManifests } from "./context-picker/manifests.js";
import { entityPickerPropertyEditorManifests } from "./entity-picker/manifests.js";
import { entityPropertyPickerPropertyEditorManifests } from "./entity-property-picker/manifests.js";
import { profilePickerPropertyEditorManifests } from "./profile-picker/manifests.js";
import { testEntityContextPropertyEditorManifests } from "./test-entity-context/manifests.js";

export const propertyEditorManifests = [
    ...contextPickerPropertyEditorManifests,
    ...entityPickerPropertyEditorManifests,
    ...entityPropertyPickerPropertyEditorManifests,
    ...profilePickerPropertyEditorManifests,
    ...testEntityContextPropertyEditorManifests
];

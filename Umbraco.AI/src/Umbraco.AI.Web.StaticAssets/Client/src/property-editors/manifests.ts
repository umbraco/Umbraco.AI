import { contextPickerPropertyEditorManifests } from "./context-picker/manifests.js";
import { entityPickerPropertyEditorManifests } from "./entity-picker/manifests.js";
import { entityPropertyPickerPropertyEditorManifests } from "./entity-property-picker/manifests.js";
import { profilePickerPropertyEditorManifests } from "./profile-picker/manifests.js";
import { mockEntityPropertyEditorManifests } from "./mock-entity/manifests.js";

export const propertyEditorManifests = [
    ...contextPickerPropertyEditorManifests,
    ...entityPickerPropertyEditorManifests,
    ...entityPropertyPickerPropertyEditorManifests,
    ...profilePickerPropertyEditorManifests,
    ...mockEntityPropertyEditorManifests
];

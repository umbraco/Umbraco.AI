import { contextPickerPropertyEditorManifests } from "./context-picker/manifests.js";
import { profilePickerPropertyEditorManifests } from "./profile-picker/manifests.js";

export const propertyEditorManifests = [
    ...contextPickerPropertyEditorManifests,
    ...profilePickerPropertyEditorManifests
];

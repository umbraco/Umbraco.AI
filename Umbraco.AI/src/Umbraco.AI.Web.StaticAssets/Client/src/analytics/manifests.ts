import { analyticsMenuManifests } from "./menu/manifests.js";
import { analyticsWorkspaceManifests } from "./workspace/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_VIEW_ANALYTICS } from "../permissions/constants.js";

export const analyticsManifests: Array<UmbExtensionManifest> = withAreaPermission(UAI_PERMISSION_VERB_VIEW_ANALYTICS, [
    ...analyticsMenuManifests,
    ...analyticsWorkspaceManifests,
]);

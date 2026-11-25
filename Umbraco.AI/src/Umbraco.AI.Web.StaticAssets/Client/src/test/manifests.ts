import { testMenuManifests } from "./menu/manifests.js";
import { testWorkspaceManifests } from "./workspace/manifests.js";
import { testEntityActionManifests } from "./entity-actions/manifests.js";
import { testRepositoryManifests } from "./repository/manifests.js";
import { testCollectionManifests } from "./collection/manifests.js";
import { testModalManifests } from "./modals/manifests.js";
import { mockEntityManifests } from "./components/mock-entity/manifests.js";
import { withAreaPermission } from "../permissions/area-conditions.js";
import { UAI_PERMISSION_VERB_MANAGE_TESTS } from "../permissions/constants.js";

export const testManifests: Array<UmbExtensionManifest> = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_TESTS, [
        ...testMenuManifests,
        ...testCollectionManifests,
        ...testWorkspaceManifests,
        ...testEntityActionManifests,
    ]),
    ...testRepositoryManifests,
    ...testModalManifests,
    ...mockEntityManifests,
];

import { withAreaPermission } from "@umbraco-ai/core";
import { promptCollectionManifests } from "./collection/manifests.js";
import { promptEntityActionManifests } from "./entity-actions/manifests.js";
import { promptMenuManifests } from "./menu/manifests.js";
import { UAI_PERMISSION_VERB_MANAGE_PROMPTS } from "./permissions/constants.js";
import { promptPermissionManifests } from "./permissions/manifests.js";
import { promptRepositoryManifests } from "./repository/manifests.js";
import { promptWorkspaceManifests } from "./workspace/manifests.js";
import { promptPropertyActionManifests } from "./property-actions/manifests.js";
import { promptTiptapManifests } from "./tiptap/manifests.js";

export const promptManifests = [
    ...withAreaPermission(UAI_PERMISSION_VERB_MANAGE_PROMPTS, [
        ...promptCollectionManifests,
        ...promptEntityActionManifests,
        ...promptMenuManifests,
        ...promptWorkspaceManifests,
    ]),
    ...promptPermissionManifests,
    ...promptRepositoryManifests,
    ...promptPropertyActionManifests,
    ...promptTiptapManifests,
];

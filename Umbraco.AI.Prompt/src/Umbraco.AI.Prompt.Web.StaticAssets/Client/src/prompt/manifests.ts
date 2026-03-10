import { promptCollectionManifests } from "./collection/manifests.js";
import { promptEntityActionManifests } from "./entity-actions/manifests.js";
import { promptMenuManifests } from "./menu/manifests.js";
import { promptRepositoryManifests } from "./repository/manifests.js";
import { promptWorkspaceManifests } from "./workspace/manifests.js";
import { promptPropertyActionManifests } from "./property-actions/manifests.js";
import { promptTiptapManifests } from "./tiptap/manifests.js";

export const promptManifests = [
    ...promptCollectionManifests,
    ...promptEntityActionManifests,
    ...promptMenuManifests,
    ...promptRepositoryManifests,
    ...promptWorkspaceManifests,
    ...promptPropertyActionManifests,
    ...promptTiptapManifests,
];

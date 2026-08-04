import { projectWorkspaceManifests } from "./workspace/manifests.js";
import { projectEntityActionManifests } from "./entity-actions/manifests.js";

/** All project-feature extensions (workspace views/actions + entity actions), aggregated. */
export const projectManifests: UmbExtensionManifest[] = [...projectWorkspaceManifests, ...projectEntityActionManifests];

import { manifests as knowledgeSetRootManifests } from "./knowledge-set-root/manifests.js";
import { manifests as knowledgeSetManifests } from "./knowledge-set/manifests.js";

export const knowledgeSetWorkspaceManifests: Array<UmbExtensionManifest> = [
    ...knowledgeSetRootManifests,
    ...knowledgeSetManifests,
];

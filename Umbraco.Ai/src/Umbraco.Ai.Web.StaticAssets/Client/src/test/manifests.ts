import { testCollectionManifests } from "./collection/manifests.js";
import { testMenuManifests } from "./menu/manifests.js";
import { testRepositoryManifests } from "./repository/manifests.js";

export const testManifests = [
    ...testCollectionManifests,
    ...testMenuManifests,
    ...testRepositoryManifests,
];

import { manifests as entityToolManifests } from "./entity/manifests.js";
import { manifests as exampleToolManifests } from "./examples/manifests.js";
import { manifests as umbracoToolManifests } from "./umbraco/manifests.js";

export const manifests = [
    ...entityToolManifests,
    ...exampleToolManifests,
    ...umbracoToolManifests,
];

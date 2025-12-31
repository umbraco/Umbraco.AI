import { UAI_AGENT_TOOL_DEFAULT_KIND_MANIFEST } from "./default/default.tool.kind.js";
import { manifests as entityToolManifests } from "./entity/manifests.js";
import { manifests as exampleToolManifests } from "./examples/manifests.js";

export const manifests = [
	UAI_AGENT_TOOL_DEFAULT_KIND_MANIFEST,
	...entityToolManifests,
	...exampleToolManifests,
];

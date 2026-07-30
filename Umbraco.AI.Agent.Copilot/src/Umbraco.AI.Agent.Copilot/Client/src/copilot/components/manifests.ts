import { sidebarManifests } from "./sidebar/manifests.js";
import { copilotFabManifests } from "./copilot-fab/manifests.js";

// The global header-app button is disabled for now in favour of the contextual FAB
// (copilotFabManifests, injected into supported workspaces). To restore it, re-add
// `...headerAppManifests` here (import from "./header-app/manifests.js") — the files are kept intact.
export const componentManifests = [...sidebarManifests, ...copilotFabManifests];

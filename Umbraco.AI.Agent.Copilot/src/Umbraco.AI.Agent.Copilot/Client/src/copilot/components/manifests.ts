import { sidebarManifests } from "./sidebar/manifests.js";

// The global header-app button is disabled for now in favour of the contextual FAB. The FAB is not
// registered via a manifest — the sidebar entry point mounts it and UaiCopilotFabController drives
// its visibility from the copilot's detected entities. To restore the header button, re-add
// `...headerAppManifests` here (import from "./header-app/manifests.js") — the files are kept intact.
export const componentManifests = [...sidebarManifests];

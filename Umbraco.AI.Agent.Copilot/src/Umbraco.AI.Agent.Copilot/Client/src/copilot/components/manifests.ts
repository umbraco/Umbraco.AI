import { sidebarManifests } from "./sidebar/manifests.js";

// The contextual FAB is not registered via a manifest — the sidebar entry point mounts it and
// UaiCopilotFabController drives its visibility from the copilot's detected entities.
export const componentManifests = [...sidebarManifests];

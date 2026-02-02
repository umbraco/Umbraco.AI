import type { ManifestUaiAgentApprovalElement } from "./uai-agent-approval-element.extension.js";

/**
 * Default approval element manifest.
 * Standard approve/deny buttons for simple confirmations.
 */
const defaultApprovalManifest: ManifestUaiAgentApprovalElement = {
  type: "uaiAgentApprovalElement",
  alias: "Uai.AgentApprovalElement.Default",
  name: "Default Approval Element",
  element: () => import("./elements/default.element.js"),
  meta: {
    label: "Default Approval",
    description: "Standard approve/deny buttons for confirmations",
    icon: "icon-check",
  },
};

export const manifests = [
  defaultApprovalManifest
];

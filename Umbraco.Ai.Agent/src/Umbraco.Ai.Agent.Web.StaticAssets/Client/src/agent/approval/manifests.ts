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

/**
 * Input approval element manifest.
 * Text input for collecting user responses.
 */
const inputApprovalManifest: ManifestUaiAgentApprovalElement = {
  type: "uaiAgentApprovalElement",
  alias: "Uai.AgentApprovalElement.Input",
  name: "Input Approval Element",
  element: () => import("./elements/input.element.js"),
  meta: {
    label: "Input Approval",
    description: "Text input for user responses",
    icon: "icon-edit",
  },
};

/**
 * Choice approval element manifest.
 * Multiple choice selection for structured responses.
 */
const choiceApprovalManifest: ManifestUaiAgentApprovalElement = {
  type: "uaiAgentApprovalElement",
  alias: "Uai.AgentApprovalElement.Choice",
  name: "Choice Approval Element",
  element: () => import("./elements/choice.element.js"),
  meta: {
    label: "Choice Approval",
    description: "Multiple choice selection for structured responses",
    icon: "icon-bulleted-list",
  },
};

export const manifests = [
  defaultApprovalManifest,
  inputApprovalManifest,
  choiceApprovalManifest,
];

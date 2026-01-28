import type {
  ManifestElement,
} from "@umbraco-cms/backoffice/extension-api";
import type { UmbControllerHostElement } from "@umbraco-cms/backoffice/controller-api";

/**
 * Props interface for agent approval elements.
 * All approval elements receive these standardized props.
 */
export interface UaiAgentApprovalElementProps {
  /** Tool arguments from the LLM */
  args: Record<string, unknown>;
  /** Static config from tool manifest (optional overrides/defaults) */
  config: Record<string, unknown>;
  /** Callback to respond - MUST be called to complete the approval */
  respond: (result: unknown) => void;
}

/**
 * Base element type for approval render elements.
 */
export type UaiAgentApprovalElement = UmbControllerHostElement &
  UaiAgentApprovalElementProps;

/**
 * Manifest for Agent Approval UI elements.
 *
 * Approval elements are reusable UI components that agent tools can reference
 * by alias for human-in-the-loop interactions.
 *
 * Props interface:
 * - `args` - Tool arguments from the LLM (e.g., contentId, contentName)
 * - `config` - Static config from tool manifest (e.g., custom title/message)
 * - `respond` - Callback to return the user's response
 *
 * Priority order for display values: `config` → `args` → localized defaults
 *
 * @example
 * ```typescript
 * // Built-in default approval
 * {
 *   type: 'uaiAgentApprovalElement',
 *   alias: 'Uai.AgentApprovalElement.Default',
 *   name: 'Default Approval Element',
 *   element: () => import('./elements/default.element.js'),
 *   meta: {
 *     label: 'Default Approval',
 *     description: 'Standard approve/deny buttons',
 *   }
 * }
 *
 * // Custom approval element
 * {
 *   type: 'uaiAgentApprovalElement',
 *   alias: 'MyProject.AgentApprovalElement.PublishPreview',
 *   name: 'Publish Preview Approval',
 *   element: () => import('./publish-preview.element.js'),
 *   meta: {
 *     label: 'Publish Preview',
 *     description: 'Shows preview before publishing',
 *     icon: 'icon-globe',
 *   }
 * }
 * ```
 */
export interface ManifestUaiAgentApprovalElement
  extends ManifestElement<UaiAgentApprovalElement> {
  type: "uaiAgentApprovalElement";
  meta: {
    /** Display label for the approval type */
    label: string;
    /** Description of when to use this approval type */
    description?: string;
    /** Icon for the approval type */
    icon?: string;
  };
}

declare global {
  interface UmbExtensionManifestMap {
    uaiAgentApprovalElement: ManifestUaiAgentApprovalElement;
  }
}

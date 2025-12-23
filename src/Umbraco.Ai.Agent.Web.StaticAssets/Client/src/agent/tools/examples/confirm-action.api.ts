import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "../uai-agent-tool.extension.js";

/**
 * HITL approval response shape from approval elements.
 */
interface ApprovalResponse {
  approved?: boolean;
}

/**
 * Example HITL tool: Confirm Action
 *
 * This tool demonstrates the Human-in-the-Loop approval flow.
 * Before executing, it pauses for user confirmation.
 * The LLM provides the action description, and the user approves or denies.
 *
 * The `__approval` field is automatically injected by the tool-renderer
 * after the user responds to the approval dialog.
 */
export default class ConfirmActionApi extends UmbControllerBase implements UaiAgentToolApi {
  async execute(args: Record<string, unknown>): Promise<string> {
    // The __approval field contains the user's response
    const approval = args.__approval as ApprovalResponse | undefined;

    if (!approval?.approved) {
      return JSON.stringify({
        success: false,
        reason: "User denied the action",
        action: args.action,
      });
    }

    // User approved - simulate performing the action
    // In a real tool, this would call an API, modify data, etc.
    return JSON.stringify({
      success: true,
      message: `Action "${args.action}" was approved and executed`,
      action: args.action,
      timestamp: new Date().toISOString(),
    });
  }
}

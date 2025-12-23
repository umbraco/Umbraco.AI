import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "../uai-agent-tool.extension.js";

/**
 * HITL input response shape from input approval element.
 */
interface InputResponse {
  input?: string;
  cancelled?: boolean;
}

/**
 * Example HITL tool: Get User Feedback
 *
 * This tool demonstrates the Human-in-the-Loop input flow.
 * It prompts the user for text input before continuing.
 * The LLM provides the question context, and the user types their response.
 *
 * The `__approval` field is automatically injected by the tool-renderer
 * after the user submits their input.
 */
export default class GetUserFeedbackApi extends UmbControllerBase implements UaiAgentToolApi {
  async execute(args: Record<string, unknown>): Promise<string> {
    // The __approval field contains the user's response
    const response = args.__approval as InputResponse | undefined;

    if (response?.cancelled) {
      return JSON.stringify({
        success: false,
        reason: "User cancelled the input",
        topic: args.topic,
      });
    }

    if (!response?.input) {
      return JSON.stringify({
        success: false,
        reason: "No input provided",
        topic: args.topic,
      });
    }

    // Return the user's feedback to the LLM
    return JSON.stringify({
      success: true,
      topic: args.topic,
      feedback: response.input,
      timestamp: new Date().toISOString(),
    });
  }
}

import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UAI_COPILOT_CONTEXT } from "../../../copilot/copilot.context.js";
import type { UaiAgentToolApi } from "../uai-agent-tool.extension.js";

/**
 * Frontend tool: Set Property Value
 *
 * Updates a property value on the currently selected entity in the workspace.
 * Changes are staged (not persisted) - user must click Save.
 *
 * Only supports TextBox and TextArea property editors currently.
 */
export default class SetPropertyValueApi extends UmbControllerBase implements UaiAgentToolApi {
	async execute(args: Record<string, unknown>): Promise<string> {
		const alias = args.alias as string | undefined;
		const value = args.value;
		const culture = args.culture as string | undefined;
		const segment = args.segment as string | undefined;

		// Validate required args
		if (!alias) {
			return JSON.stringify({
				success: false,
				error: "Missing required argument: alias",
			});
		}

		if (value === undefined) {
			return JSON.stringify({
				success: false,
				error: "Missing required argument: value",
			});
		}

		// Get the copilot context
		const copilotContext = await this.getContext(UAI_COPILOT_CONTEXT);
		if (!copilotContext) {
			return JSON.stringify({
				success: false,
				error: "Copilot context not available",
			});
		}

		// Apply the property change
		const result = await copilotContext.applyPropertyChange({
			alias,
			value,
			culture,
			segment,
		});

		return JSON.stringify(result);
	}
}

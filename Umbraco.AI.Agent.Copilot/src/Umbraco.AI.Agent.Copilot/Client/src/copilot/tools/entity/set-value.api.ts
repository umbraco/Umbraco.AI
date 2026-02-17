import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import { UAI_ENTITY_CONTEXT, type UaiAgentToolApi } from "@umbraco-ai/agent-ui";

/**
 * Frontend tool: Set Value
 *
 * Updates a value on the currently selected entity in the workspace.
 * Changes are staged (not persisted) - user must click Save.
 *
 * Consumes the shared UAI_ENTITY_CONTEXT contract, which is provided by the
 * copilot (wrapping workspace entity state) and could be provided by chat
 * (via a side-drawer editor) in the future.
 */
export default class SetValueApi extends UmbControllerBase implements UaiAgentToolApi {
    async execute(args: Record<string, unknown>): Promise<string> {
        const path = args.path as string | undefined;
        const value = args.value;

        // Validate required args
        if (!path) {
            return JSON.stringify({
                success: false,
                error: "Missing required argument: path",
            });
        }

        if (value === undefined) {
            return JSON.stringify({
                success: false,
                error: "Missing required argument: value",
            });
        }

        // Get the entity context (shared contract from agent-ui)
        const entityContext = await this.getContext(UAI_ENTITY_CONTEXT);
        if (!entityContext) {
            return JSON.stringify({
                success: false,
                error: "Entity context not available. This tool requires an active entity editor.",
            });
        }

        // Apply the value change via the shared context
        try {
            entityContext.setValue(path, value);

            // Success - value change was staged
            return JSON.stringify({
                success: true,
                message: `Value "${path}" updated. Changes are staged - user must save to persist.`,
            });
        } catch (error) {
            return JSON.stringify({
                success: false,
                error: error instanceof Error ? error.message : "Unknown error applying value change",
            });
        }
    }
}

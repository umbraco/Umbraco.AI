import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "@umbraco-ai/agent-ui";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "../../contexts/entity-adapter.context-token.js";

/**
 * Frontend tool: save.
 *
 * Persists the staged changes on the currently selected entity (the equivalent of the user
 * clicking the workspace's Save button). Returns a structured error for entity types without a
 * direct save action — most commonly when a block workspace is selected, since block changes
 * persist through their parent document.
 */
export default class SaveApi extends UmbControllerBase implements UaiAgentToolApi {
    async execute(_args: Record<string, unknown>): Promise<string> {
        const adapterContext = await this.getContext(UAI_ENTITY_ADAPTER_CONTEXT);
        if (!adapterContext) {
            return JSON.stringify({
                success: false,
                error: {
                    code: "no-adapter-context",
                    message: "Entity adapter context not available. This tool requires an active entity editor.",
                },
            });
        }

        const result = await adapterContext.saveSelectedEntity();

        if (!result.success) {
            return JSON.stringify({
                success: false,
                error: {
                    code: "save-failed",
                    message: result.error ?? "Save failed.",
                },
            });
        }

        return JSON.stringify({
            success: true,
            message: "Staged changes saved.",
        });
    }
}

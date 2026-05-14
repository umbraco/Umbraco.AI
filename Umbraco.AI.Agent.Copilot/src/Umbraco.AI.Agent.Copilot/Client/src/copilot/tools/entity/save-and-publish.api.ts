import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "@umbraco-ai/agent-ui";
import { UAI_ENTITY_ADAPTER_CONTEXT } from "../../contexts/entity-adapter.context-token.js";

/**
 * Frontend tool: save_and_publish.
 *
 * Persists staged changes and publishes the document to the public site (the equivalent of the
 * user clicking Save and publish). Only documents have a publish concept — media/blocks/etc.
 * return a structured error directing the LLM to use save instead.
 *
 * Multi-variant documents may surface a CMS variant picker modal as part of saveAndPublish; this
 * is the same UX a human gets and is intentional — the LLM cannot bypass the workspace's
 * publication confirmation flow.
 */
export default class SaveAndPublishApi extends UmbControllerBase implements UaiAgentToolApi {
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

        const result = await adapterContext.publishSelectedEntity();

        if (!result.success) {
            return JSON.stringify({
                success: false,
                error: {
                    code: "publish-failed",
                    message: result.error ?? "Publish failed.",
                },
            });
        }

        return JSON.stringify({
            success: true,
            message: "Document saved and published.",
        });
    }
}

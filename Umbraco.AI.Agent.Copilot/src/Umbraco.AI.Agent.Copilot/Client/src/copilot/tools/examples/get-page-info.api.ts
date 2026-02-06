import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UaiAgentToolApi } from "../uai-agent-tool.extension.js";

/**
 * Example frontend tool: Get Page Info
 * Returns information about the current Umbraco backoffice page.
 */
export default class GetPageInfoApi extends UmbControllerBase implements UaiAgentToolApi {
    async execute(_args: Record<string, unknown>): Promise<string> {
        const url = window.location.href;
        const pathname = window.location.pathname;
        const hash = window.location.hash;

        // Build page info object
        const info: Record<string, unknown> = {
            url,
            pathname,
            hash,
        };

        // Try to extract section from URL (e.g., /umbraco/section/content)
        const sectionMatch = pathname.match(/\/umbraco\/section\/([^/]+)/);
        if (sectionMatch) {
            info.section = sectionMatch[1];
        }

        // Try to extract entity info from hash
        if (hash) {
            // Common patterns: #/content/edit/guid, #/media/edit/guid, etc.
            const entityMatch = hash.match(/#\/([^/]+)\/([^/]+)(?:\/([^/]+))?/);
            if (entityMatch) {
                info.entityType = entityMatch[1];
                info.action = entityMatch[2];
                if (entityMatch[3]) {
                    info.entityId = entityMatch[3];
                }
            }
        }

        // Add document title as context
        info.pageTitle = document.title;

        return JSON.stringify(info, null, 2);
    }
}

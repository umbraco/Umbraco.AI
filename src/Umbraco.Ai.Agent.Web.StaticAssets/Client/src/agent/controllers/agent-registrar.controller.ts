import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UAiAgentRegistrarRepository } from "../repository/index.js";
import { generatePromptPropertyActionManifest } from "../property-actions/generate-prompt-property-action-manifest.js";

/**
 * Controller that fetches all active Agents and registers them as property actions.
 * Each prompt becomes a property action available on text-based property editors.
 */
export class UmbPromptRegistrarController extends UmbControllerBase {
    #repository: UAiAgentRegistrarRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UAiAgentRegistrarRepository(this);
    }

    /**
     * Fetches all active Agents from the server and registers them as property actions.
     */
    async registerAgents(): Promise<void> {
        const { data, error } = await this.#repository.getActiveAgents();

        if (error || !data) {
            console.warn('[UmbracoAiAgent] Failed to fetch Agents for registration:', error);
            return;
        }

        if (data.length === 0) {
            return;
        }

        // Generate manifests for each prompt with descending weight so they appear in order
        // Filter out null manifests (Agents without valid visibility)
        const manifests = data
            .map((prompt, index) => generatePromptPropertyActionManifest(prompt, 100 - index))
            .filter((manifest): manifest is NonNullable<typeof manifest> => manifest !== null);

        // Register each manifest, checking for duplicates
        manifests.forEach((manifest) => {
            if (!umbExtensionsRegistry.isRegistered(manifest.alias)) {
                umbExtensionsRegistry.register(manifest);
            }
        });

        console.log(`[UmbracoAiAgent] Registered ${manifests.length} prompt property actions`);
    }
}
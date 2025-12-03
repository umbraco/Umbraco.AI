import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UaiPromptRegistrarRepository } from "../repository/index.js";
import { generatePromptPropertyActionManifest } from "../property-actions/generate-prompt-property-action-manifest.js";

/**
 * Controller that fetches all active prompts and registers them as property actions.
 * Each prompt becomes a property action available on text-based property editors.
 */
export class UmbPromptRegistrarController extends UmbControllerBase {
    #repository: UaiPromptRegistrarRepository;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiPromptRegistrarRepository(this);
    }

    /**
     * Fetches all active prompts from the server and registers them as property actions.
     */
    async registerPrompts(): Promise<void> {
        const { data, error } = await this.#repository.getActivePrompts();

        if (error || !data) {
            console.warn('[UmbracoAiPrompt] Failed to fetch prompts for registration:', error);
            return;
        }

        if (data.length === 0) {
            return;
        }

        // Generate manifests for each prompt with descending weight so they appear in order
        const manifests = data.map((prompt, index) =>
            generatePromptPropertyActionManifest(prompt, 100 - index)
        );

        // Register each manifest, checking for duplicates
        manifests.forEach((manifest) => {
            if (!umbExtensionsRegistry.isRegistered(manifest.alias)) {
                umbExtensionsRegistry.register(manifest);
            }
        });

        console.log(`[UmbracoAiPrompt] Registered ${manifests.length} prompt property actions`);
    }
}
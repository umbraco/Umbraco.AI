import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import { UaiPromptRegistrarRepository } from "../repository/registrar/prompt-registrar.repository.js";
import type { PromptManifestEntry } from "../property-actions/types.js";

/**
 * Controller that manages prompt property action registration.
 * Subscribes to repository state and syncs to the extension registry.
 * This is a thin presentation layer - business logic is in the repository.
 */
export class UmbPromptRegistrarController extends UmbControllerBase {
    #repository: UaiPromptRegistrarRepository;
    #registeredAliases = new Set<string>();

    constructor(host: UmbControllerHost) {
        super(host);
        this.#repository = new UaiPromptRegistrarRepository(this);

        // Subscribe to repository state and sync to registry
        this.observe(
            this.#repository.promptEntries$,
            (entries) => this.#syncToRegistry(entries)
        );
    }

    /**
     * Initializes the repository by loading all active prompts.
     */
    async registerPrompts(): Promise<void> {
        await this.#repository.initialize();
    }

    /**
     * Syncs prompt manifest entries to the extension registry.
     * Sorts alphabetically by alias and assigns weights.
     * Unregisters any prompts that are no longer in the state.
     */
    #syncToRegistry(entries: Map<string, PromptManifestEntry>): void {
        const currentAliases = new Set<string>();

        // Sort alphabetically by alias and assign weights
        const sorted = Array.from(entries.values())
            .sort((a, b) => a.alias.localeCompare(b.alias));

        // Register/update all current entries
        sorted.forEach((entry, index) => {
            // Assign weight based on alphabetical position
            entry.manifest.weight = 100 - index;

            // Track this alias
            currentAliases.add(entry.manifest.alias);

            // Unregister if already registered, then re-register
            if (umbExtensionsRegistry.isRegistered(entry.manifest.alias)) {
                umbExtensionsRegistry.unregister(entry.manifest.alias);
            }
            umbExtensionsRegistry.register(entry.manifest);
        });

        // Unregister any prompts that are no longer in the state
        for (const alias of this.#registeredAliases) {
            if (!currentAliases.has(alias)) {
                umbExtensionsRegistry.unregister(alias);
            }
        }

        // Update tracked aliases
        this.#registeredAliases = currentAliases;

        console.log(`[UmbracoAIPrompt] Synced ${sorted.length} prompt property actions to registry`);
    }
}

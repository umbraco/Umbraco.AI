import { UmbRepositoryBase } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject } from '@umbraco-cms/backoffice/external/rxjs';
import type { Observable } from '@umbraco-cms/backoffice/external/rxjs';
import { UMB_ACTION_EVENT_CONTEXT } from '@umbraco-cms/backoffice/action';
import { UaiEntityActionEvent } from '@umbraco-ai/core';
import { UaiPromptRegistrarServerDataSource } from "./prompt-registrar.server.data-source.js";
import type { PromptManifestEntry } from "../../property-actions/types.js";
import { generatePromptPropertyActionManifest } from "../../property-actions/generate-prompt-property-action-manifest.js";
import { UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";

/**
 * Repository for fetching prompts for property action registration.
 * Manages observable state and listens to entity action events for reactive updates.
 */
export class UaiPromptRegistrarRepository extends UmbRepositoryBase {
    #dataSource: UaiPromptRegistrarServerDataSource;
    #promptEntries$ = new BehaviorSubject<Map<string, PromptManifestEntry>>(new Map());
    #isInitialized = false;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#dataSource = new UaiPromptRegistrarServerDataSource(host);

        // Repository listens to events directly and manages state
        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
            if (!context) return;
            context.addEventListener(UaiEntityActionEvent.CREATED, ((event: Event) => {
                if (event instanceof UaiEntityActionEvent) {
                    this.#onPromptCreatedOrUpdated(event);
                }
            }) as EventListener);
            context.addEventListener(UaiEntityActionEvent.UPDATED, ((event: Event) => {
                if (event instanceof UaiEntityActionEvent) {
                    this.#onPromptCreatedOrUpdated(event);
                }
            }) as EventListener);
            context.addEventListener(UaiEntityActionEvent.DELETED, ((event: Event) => {
                if (event instanceof UaiEntityActionEvent) {
                    this.#onPromptDeleted(event);
                }
            }) as EventListener);
        });
    }

    /**
     * Observable stream of prompt manifest entries.
     */
    get promptEntries$(): Observable<Map<string, PromptManifestEntry>> {
        return this.#promptEntries$.asObservable();
    }

    /**
     * Initializes the repository by loading all active prompts.
     */
    async initialize(): Promise<void> {
        const { data, error } = await this.#dataSource.getActivePrompts();
        if (error || !data) {
            console.warn('[UaiPromptRegistrarRepository] Failed to load prompts:', error);
            return;
        }

        const entries = new Map<string, PromptManifestEntry>();
        data.forEach((prompt) => {
            // Weight doesn't matter - will be assigned at sync time
            const manifest = generatePromptPropertyActionManifest(prompt, 100);
            if (manifest) {
                entries.set(prompt.unique, {
                    unique: prompt.unique,
                    alias: prompt.alias,
                    manifest,
                });
            }
        });

        this.#promptEntries$.next(entries);
        this.#isInitialized = true;
    }

    /**
     * Unified handler for CREATE and UPDATE - just fetch and upsert.
     */
    #onPromptCreatedOrUpdated = async (event: UaiEntityActionEvent) => {
        if (!this.#isInitialized || event.getEntityType() !== UAI_PROMPT_ENTITY_TYPE) return;

        const unique = event.getUnique();
        if (!unique) return;

        const { data: prompt, error } = await this.#dataSource.getPromptById(unique);

        if (error || !prompt) return;

        // Remove if inactive
        if (!prompt.isActive) {
            this.#removeEntry(unique);
            return;
        }

        // Generate manifest (weight will be assigned at sync time)
        const manifest = generatePromptPropertyActionManifest(prompt, 100);

        // Remove if manifest generation fails (invalid scope, etc.)
        if (!manifest) {
            this.#removeEntry(unique);
            return;
        }

        // Always add/update - no comparison needed
        this.#addOrUpdateEntry({
            unique: prompt.unique,
            alias: prompt.alias,
            manifest,
        });
    };

    /**
     * Handler for DELETE events.
     */
    #onPromptDeleted = (event: UaiEntityActionEvent) => {
        if (!this.#isInitialized || event.getEntityType() !== UAI_PROMPT_ENTITY_TYPE) return;

        const unique = event.getUnique();
        if (!unique) return;

        this.#removeEntry(unique);
    };

    /**
     * Adds or updates an entry in the observable state.
     */
    #addOrUpdateEntry(entry: PromptManifestEntry): void {
        const current = new Map(this.#promptEntries$.value);
        current.set(entry.unique, entry);
        this.#promptEntries$.next(current);
    }

    /**
     * Removes an entry from the observable state.
     */
    #removeEntry(unique: string): void {
        const current = new Map(this.#promptEntries$.value);
        if (current.delete(unique)) {
            this.#promptEntries$.next(current);
        }
    }
}

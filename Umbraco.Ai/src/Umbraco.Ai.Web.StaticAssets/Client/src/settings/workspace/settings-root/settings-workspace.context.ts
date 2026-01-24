import { UmbSubmittableWorkspaceContextBase } from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import { UAI_SETTINGS_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_SETTINGS_ROOT_ENTITY_TYPE } from "../../entity.js";
import { SettingsService } from "../../../api/sdk.gen.js";

export interface UaiSettingsModel {
    defaultChatProfileId: string | null;
    defaultEmbeddingProfileId: string | null;
}

/**
 * Workspace context for editing AI Settings.
 * Handles state management and saving.
 */
export class UaiSettingsWorkspaceContext extends UmbSubmittableWorkspaceContextBase<UaiSettingsModel> {
    public readonly IS_SETTINGS_WORKSPACE_CONTEXT = true;

    // Required by UmbSubmittableWorkspaceContextBase - settings is a singleton
    #unique = new UmbBasicState<string>("settings");
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiSettingsModel>({
        defaultChatProfileId: null,
        defaultEmbeddingProfileId: null,
    });
    readonly model = this.#model.asObservable();

    #loading = new UmbObjectState<boolean>(true);
    readonly loading = this.#loading.asObservable();

    #error = new UmbObjectState<string | null>(null);
    readonly error = this.#error.asObservable();

    constructor(host: UmbControllerHost) {
        super(host, UAI_SETTINGS_ROOT_WORKSPACE_ALIAS);
        this.#loadSettings();
    }

    async #loadSettings(): Promise<void> {
        this.#loading.setValue(true);
        this.#error.setValue(null);

        const { data, error } = await tryExecute(this, SettingsService.getSettings());

        if (error) {
            this.#error.setValue("Failed to load settings");
            this.#loading.setValue(false);
            return;
        }

        if (data) {
            this.#model.setValue({
                defaultChatProfileId: data.defaultChatProfileId ?? null,
                defaultEmbeddingProfileId: data.defaultEmbeddingProfileId ?? null,
            });
        }

        this.#loading.setValue(false);
    }

    setDefaultChatProfileId(value: string | null): void {
        const current = this.#model.getValue();
        this.#model.setValue({
            ...current,
            defaultChatProfileId: value,
        });
    }

    setDefaultEmbeddingProfileId(value: string | null): void {
        const current = this.#model.getValue();
        this.#model.setValue({
            ...current,
            defaultEmbeddingProfileId: value,
        });
    }

    getData(): UaiSettingsModel {
        return this.#model.getValue();
    }

    getUnique(): string {
        return "settings";
    }

    getEntityType(): string {
        return UAI_SETTINGS_ROOT_ENTITY_TYPE;
    }

    async submit(): Promise<void> {
        const model = this.#model.getValue();
        this.#error.setValue(null);

        const { data, error } = await tryExecute(
            this,
            SettingsService.updateSettings({
                body: {
                    defaultChatProfileId: model.defaultChatProfileId ?? undefined,
                    defaultEmbeddingProfileId: model.defaultEmbeddingProfileId ?? undefined,
                },
            })
        );

        if (error) {
            this.#error.setValue("Failed to save settings");
            throw new Error("Failed to save settings");
        }

        if (data) {
            this.#model.setValue({
                defaultChatProfileId: data.defaultChatProfileId ?? null,
                defaultEmbeddingProfileId: data.defaultEmbeddingProfileId ?? null,
            });
        }
    }
}

export { UaiSettingsWorkspaceContext as api };

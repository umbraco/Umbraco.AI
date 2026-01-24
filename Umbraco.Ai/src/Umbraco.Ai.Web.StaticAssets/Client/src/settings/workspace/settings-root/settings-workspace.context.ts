import { UmbSubmittableWorkspaceContextBase } from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UMB_NOTIFICATION_CONTEXT } from "@umbraco-cms/backoffice/notification";
import { UAI_SETTINGS_ROOT_WORKSPACE_ALIAS } from "../../constants.js";
import { UAI_SETTINGS_ROOT_ENTITY_TYPE } from "../../entity.js";
import { settingsRepository } from "../../repository/settings.repository.js";
import type { UaiSettingsModel } from "../../types.js";

export type { UaiSettingsModel } from "../../types.js";

/**
 * Workspace context for editing AI Settings.
 * Handles state management and saving.
 */
export class UaiSettingsWorkspaceContext extends UmbSubmittableWorkspaceContextBase<UaiSettingsModel> {
    public readonly IS_SETTINGS_WORKSPACE_CONTEXT = true;

    #notificationContext?: typeof UMB_NOTIFICATION_CONTEXT.TYPE;

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

    constructor(host: UmbControllerHost) {
        super(host, UAI_SETTINGS_ROOT_WORKSPACE_ALIAS);

        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (context) => {
            this.#notificationContext = context;
        });

        this.#loadSettings();
    }

    async #loadSettings(): Promise<void> {
        this.#loading.setValue(true);

        try {
            const model = await settingsRepository.get();
            this.#model.setValue(model);
        } catch {
            this.#notificationContext?.peek("danger", {
                data: { message: "Failed to load settings" },
            });
        } finally {
            this.#loading.setValue(false);
        }
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

        try {
            const saved = await settingsRepository.save(model);
            this.#model.setValue(saved);
        } catch {
            this.#notificationContext?.peek("danger", {
                data: { message: "Failed to save settings" },
            });
            throw new Error("Failed to save settings");
        }
    }
}

export { UaiSettingsWorkspaceContext as api };

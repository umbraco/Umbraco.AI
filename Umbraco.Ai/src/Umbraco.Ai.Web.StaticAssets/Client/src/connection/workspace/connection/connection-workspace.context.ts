import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import {
    UmbWorkspaceRouteManager,
    UmbSubmittableWorkspaceContextBase,
    UmbWorkspaceIsNewRedirectController,
    UmbWorkspaceIsNewRedirectControllerAlias,
} from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext } from "@umbraco-cms/backoffice/validation";
import { UaiConnectionDetailRepository } from "../../repository/detail/connection-detail.repository.js";
import { UAI_CONNECTION_WORKSPACE_ALIAS, UAI_CONNECTION_ENTITY_TYPE } from "../../constants.js";
import type { UaiConnectionDetailModel } from "../../types.js";
import type { UaiCommand } from "../../../core/command/command.base.js";
import { UaiCommandStore } from "../../../core/command/command.store.js";
import {
    UAI_EMPTY_GUID,
    UaiVersionComparisonResponse,
    UaiVersionHistoryResponse,
    type UaiVersionableEntityWorkspaceContext
} from "../../../core/index.js";
import { UaiConnectionWorkspaceEditorElement } from "./connection-workspace-editor.element.js";
import { UaiEntityDeletedRedirectController } from "../../../core/workspace/entity-deleted-redirect.controller.js";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../connection-root/paths.js";
import { map } from "@umbraco-cms/backoffice/external/rxjs";
import { UaiConnectionVersionHistoryRepository } from "../../repository/index.js";

/**
 * Workspace context for editing Connection entities.
 * Handles CRUD operations, state management, and command tracking.
 */
export class UaiConnectionWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiConnectionDetailModel>
    implements UmbRoutableWorkspaceContext, UaiVersionableEntityWorkspaceContext {
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiConnectionDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    /**
     * Observable for the current version number.
     * Returns undefined for new entities.
     */
    readonly version = this.#model.asObservable().pipe(
        map((m) => m?.version)
    );
    
    #repository: UaiConnectionDetailRepository;
    #versionHistoryRepository: UaiConnectionVersionHistoryRepository;
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_CONNECTION_WORKSPACE_ALIAS);

        this.#repository = new UaiConnectionDetailRepository(this);
        this.#versionHistoryRepository = new UaiConnectionVersionHistoryRepository(this);
        this.addValidationContext(new UmbValidationContext(this));

        this.#entityContext.setEntityType(UAI_CONNECTION_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create/:providerAlias",
                component: UaiConnectionWorkspaceEditorElement,
                setup: async (_component, info) => {
                    const providerAlias = info.match.params.providerAlias;
                    await this.scaffold(providerAlias);
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!
                    );
                },
            },
            {
                path: "edit/:unique",
                component: UaiConnectionWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    this.load(info.match.params.unique);

                    // Redirect to collection view when entity is deleted
                    new UaiEntityDeletedRedirectController(this, {
                        getUnique: () => this.getUnique(),
                        getEntityType: () => this.getEntityType(),
                        collectionPath: UAI_CONNECTION_ROOT_WORKSPACE_PATH,
                    });
                },
            },
        ]);
    }

    protected resetState(): void {
        super.resetState();
        this.#unique.setValue(undefined);
        this.#model.setValue(undefined);
        this.#commandStore.reset();
    }

    /**
     * Creates a scaffold for a new connection.
     */
    async scaffold(providerId?: string) {
        this.resetState();
        const { data } = await this.#repository.createScaffold({ providerId });
        if (data) {
            this.#unique.setValue(UAI_EMPTY_GUID);
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    /**
     * Loads an existing connection by ID.
     */
    async load(id: string) {
        this.resetState();
        const { data, asObservable } = await this.#repository.requestByUnique(id);

        if (asObservable) {
            this.observe(
                asObservable(),
                (model) => {
                    if (model) {
                        this.#unique.setValue(model.unique);
                        const newModel = structuredClone(model);
                        // Replay any pending commands
                        this.#commandStore.getAll().forEach((command) => command.execute(newModel));
                        this.#model.setValue(newModel);
                        this.setIsNew(false);
                    }
                },
                "_observeModel"
            );
        }

        return data;
    }

    /**
     * Handles a command to update the model.
     * Commands are tracked for replay after model refresh.
     */
    handleCommand(command: UaiCommand) {
        const currentValue = this.#model.getValue();
        if (currentValue) {
            const newValue = structuredClone(currentValue);
            command.execute(newValue);
            this.#model.setValue(newValue);
            this.#commandStore.add(command);
        }
    }

    getData(): UaiConnectionDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_CONNECTION_ENTITY_TYPE;
    }

    /**
     * Saves the connection (create or update).
     */
    async submit() {
        const model = this.#model.getValue();
        if (!model) return;

        // Mute command store during submit
        this.#commandStore.mute();

        try {
            if (this.getIsNew()) {
                const { data, error } = await this.#repository.create(model);

                if (error) {
                    throw error;
                }

                if (data) {
                    this.#unique.setValue(data.unique);
                    this.#model.setValue(data);
                }
            } else {
                const { error } = await this.#repository.save(model);

                if (error) {
                    throw error;
                }
            }

            this.#commandStore.reset();
            this.setIsNew(false);
        } finally {
            this.#commandStore.unmute();
        }
    }

    // #region UaiVersionableEntityWorkspaceContext implementation

    /**
     * Gets the version history for this profile.
     * @param skip - Number of versions to skip (for pagination).
     * @param take - Number of versions to return.
     * @returns The version history response.
     */
    async getVersionHistory(skip: number, take: number): Promise<UaiVersionHistoryResponse | undefined> {
        const unique = this.getUnique();
        if (!unique || unique === UAI_EMPTY_GUID) return undefined;
        return this.#versionHistoryRepository.getVersionHistory(unique, skip, take);
    }

    /**
     * Compares two versions of this profile.
     * @param fromVersion - The source version number.
     * @param toVersion - The target version number.
     * @returns The comparison response with property changes.
     */
    async compareVersions(fromVersion: number, toVersion: number): Promise<UaiVersionComparisonResponse | undefined> {
        const unique = this.getUnique();
        if (!unique || unique === UAI_EMPTY_GUID) return undefined;
        return this.#versionHistoryRepository.compareVersions(unique, fromVersion, toVersion);
    }

    /**
     * Rolls back this profile to a previous version.
     * Reloads the profile data after rollback.
     * @param version - The version number to rollback to.
     */
    async rollbackToVersion(version: number): Promise<void> {
        const unique = this.getUnique();
        if (!unique || unique === UAI_EMPTY_GUID) return;
        const success = await this.#versionHistoryRepository.rollback(unique, version);
        if (success) {
            // Reload the profile to get the updated data
            await this.load(unique);
        }
    }

    // #endregion
}

export { UaiConnectionWorkspaceContext as api };

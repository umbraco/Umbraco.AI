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
import { UaiProfileDetailRepository } from "../../repository/detail/profile-detail.repository.js";
import { UAI_PROFILE_WORKSPACE_ALIAS, UAI_PROFILE_ENTITY_TYPE } from "../../constants.js";
import type { UaiProfileDetailModel } from "../../types.js";
import type { UaiCommand } from "../../../core/command/command.base.js";
import { UaiCommandStore } from "../../../core/command/command.store.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";
import { UaiProfileWorkspaceEditorElement } from "./profile-workspace-editor.element.js";
import { UaiEntityDeletedRedirectController } from "../../../core/workspace/entity-deleted-redirect.controller.js";
import { UAI_PROFILE_ROOT_WORKSPACE_PATH } from "../profile-root/paths.js";

/**
 * Workspace context for editing Profile entities.
 * Handles CRUD operations, state management, and command tracking.
 */
export class UaiProfileWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiProfileDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiProfileDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiProfileDetailRepository;
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_PROFILE_WORKSPACE_ALIAS);

        this.#repository = new UaiProfileDetailRepository(this);
        this.addValidationContext(new UmbValidationContext(this));

        this.#entityContext.setEntityType(UAI_PROFILE_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create/:capability",
                component: UaiProfileWorkspaceEditorElement,
                setup: async (_component, info) => {
                    const capability = info.match.params.capability;
                    await this.scaffold(capability);
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!
                    );
                },
            },
            {
                path: "edit/:unique",
                component: UaiProfileWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    this.load(info.match.params.unique);

                    // Redirect to collection view when entity is deleted
                    new UaiEntityDeletedRedirectController(this, {
                        getUnique: () => this.getUnique(),
                        getEntityType: () => this.getEntityType(),
                        collectionPath: UAI_PROFILE_ROOT_WORKSPACE_PATH,
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
     * Creates a scaffold for a new profile.
     */
    async scaffold(capability?: string) {
        this.resetState();
        const { data } = await this.#repository.createScaffold({ capability });
        if (data) {
            this.#unique.setValue(UAI_EMPTY_GUID);
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    /**
     * Loads an existing profile by ID.
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
     * Reloads the current connection.
     */
    async reload() {
        const unique = this.getUnique();
        if (unique) {
            await this.load(unique);
        }
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

    getData(): UaiProfileDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_PROFILE_ENTITY_TYPE;
    }

    /**
     * Saves the profile (create or update).
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
                const { data, error } = await this.#repository.save(model);

                if (error) {
                    throw error;
                }

                if (data) {
                    this.#model.setValue(data);
                }
            }

            this.#commandStore.reset();
            this.setIsNew(false);
        } finally {
            this.#commandStore.unmute();
        }
    }

}

export { UaiProfileWorkspaceContext as api };

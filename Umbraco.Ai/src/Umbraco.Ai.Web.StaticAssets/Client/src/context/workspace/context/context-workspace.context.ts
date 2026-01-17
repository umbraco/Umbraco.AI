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
import { UaiContextDetailRepository } from "../../repository/detail/context-detail.repository.js";
import { UAI_CONTEXT_WORKSPACE_ALIAS, UAI_CONTEXT_ENTITY_TYPE } from "../../constants.js";
import type { UaiContextDetailModel } from "../../types.js";
import type { UaiCommand } from "../../../core/command/command.base.js";
import { UaiCommandStore } from "../../../core/command/command.store.js";
import { UAI_EMPTY_GUID } from "../../../core/index.js";
import { UaiContextWorkspaceEditorElement } from "./context-workspace-editor.element.js";
import { UaiEntityDeletedRedirectController } from "../../../core/workspace/entity-deleted-redirect.controller.js";
import { UAI_CONTEXT_ROOT_WORKSPACE_PATH } from "../context-root/paths.js";

/**
 * Workspace context for editing Context entities.
 * Handles CRUD operations, state management, and command tracking.
 */
export class UaiContextWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiContextDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiContextDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiContextDetailRepository;
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_CONTEXT_WORKSPACE_ALIAS);

        this.#repository = new UaiContextDetailRepository(this);
        this.addValidationContext(new UmbValidationContext(this));

        this.#entityContext.setEntityType(UAI_CONTEXT_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create",
                component: UaiContextWorkspaceEditorElement,
                setup: async () => {
                    await this.scaffold();
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!
                    );
                },
            },
            {
                path: "edit/:unique",
                component: UaiContextWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    this.load(info.match.params.unique);

                    // Redirect to collection view when entity is deleted
                    new UaiEntityDeletedRedirectController(this, {
                        getUnique: () => this.getUnique(),
                        getEntityType: () => this.getEntityType(),
                        collectionPath: UAI_CONTEXT_ROOT_WORKSPACE_PATH,
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
     * Creates a scaffold for a new context.
     */
    async scaffold() {
        this.resetState();
        const { data } = await this.#repository.createScaffold({});
        if (data) {
            this.#unique.setValue(UAI_EMPTY_GUID);
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    /**
     * Loads an existing context by ID.
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

    getData(): UaiContextDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_CONTEXT_ENTITY_TYPE;
    }

    /**
     * Saves the context (create or update).
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
}

export { UaiContextWorkspaceContext as api };

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
import type { UaiCommand } from "@umbraco-ai/core";
import { UaiCommandStore, UAI_EMPTY_GUID, UaiEntityDeletedRedirectController } from "@umbraco-ai/core";
import { UaiOrchestrationDetailRepository } from "../../repository/detail/orchestration-detail.repository.js";
import { UAI_ORCHESTRATION_WORKSPACE_ALIAS, UAI_ORCHESTRATION_ENTITY_TYPE } from "../../constants.js";
import type { UaiOrchestrationDetailModel } from "../../types.js";
import { UaiOrchestrationWorkspaceEditorElement } from "./orchestration-workspace-editor.element.js";
import { UAI_ORCHESTRATION_ROOT_WORKSPACE_PATH } from "../orchestration-root/paths.js";

/**
 * Workspace context for editing Orchestration entities.
 * Handles CRUD operations and state management.
 */
export class UaiOrchestrationWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiOrchestrationDetailModel>
    implements UmbRoutableWorkspaceContext
{
    public readonly IS_ORCHESTRATION_WORKSPACE_CONTEXT = true;
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiOrchestrationDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiOrchestrationDetailRepository;
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);
    #validationContext = new UmbValidationContext(this);

    get validation() {
        return this.#validationContext;
    }

    constructor(host: UmbControllerHost) {
        super(host, UAI_ORCHESTRATION_WORKSPACE_ALIAS);

        this.#repository = new UaiOrchestrationDetailRepository(this);
        this.addValidationContext(this.#validationContext);

        this.#entityContext.setEntityType(UAI_ORCHESTRATION_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        // Redirect to collection when entity is deleted
        new UaiEntityDeletedRedirectController(this, {
            getUnique: () => this.getUnique(),
            getEntityType: () => this.getEntityType(),
            collectionPath: UAI_ORCHESTRATION_ROOT_WORKSPACE_PATH,
        });

        this.routes.setRoutes([
            {
                path: "create",
                component: UaiOrchestrationWorkspaceEditorElement,
                setup: async () => {
                    await this.scaffold();
                    new UmbWorkspaceIsNewRedirectController(
                        this,
                        this,
                        this.getHostElement().shadowRoot!.querySelector("umb-router-slot")!,
                    );
                },
            },
            {
                path: "edit/:unique",
                component: UaiOrchestrationWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.removeUmbControllerByAlias(UmbWorkspaceIsNewRedirectControllerAlias);
                    this.load(info.match.params.unique);
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

    async scaffold() {
        this.resetState();
        const { data } = await this.#repository.createScaffold();
        if (data) {
            this.#unique.setValue(UAI_EMPTY_GUID);
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

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
                        this.#commandStore.getAll().forEach((command) => command.execute(newModel));
                        this.#model.setValue(newModel);
                        this.setIsNew(false);
                    }
                },
                "_observeModel",
            );
        }

        return data;
    }

    async reload() {
        const unique = this.#unique.getValue();
        if (unique) {
            await this.load(unique);
        }
    }

    handleCommand(command: UaiCommand) {
        const currentValue = this.#model.getValue();
        if (currentValue) {
            const newValue = structuredClone(currentValue);
            command.execute(newValue);
            this.#model.setValue(newValue);
            this.#commandStore.add(command);
        }
    }

    getData(): UaiOrchestrationDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_ORCHESTRATION_ENTITY_TYPE;
    }

    async submit() {
        const model = this.#model.getValue();
        if (!model) return;

        try {
            await this.#validationContext.validate();
        } catch {
            this.#validationContext.focusFirstInvalidElement();
            return;
        }

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

export { UaiOrchestrationWorkspaceContext as api };

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
import { UaiPromptDetailRepository } from "../../repository/detail/prompt-detail.repository.js";
import { UAI_PROMPT_WORKSPACE_ALIAS, UAI_PROMPT_ENTITY_TYPE } from "../../constants.js";
import type { UaiPromptDetailModel } from "../../types.js";
import { UaiPromptWorkspaceEditorElement } from "./prompt-workspace-editor.element.js";

const UAI_EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

/**
 * Workspace context for editing Prompt entities.
 * Handles CRUD operations and state management.
 */
export class UaiPromptWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiPromptDetailModel>
    implements UmbRoutableWorkspaceContext
{
    public readonly IS_PROMPT_WORKSPACE_CONTEXT = true;
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiPromptDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiPromptDetailRepository;
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_PROMPT_WORKSPACE_ALIAS);

        this.#repository = new UaiPromptDetailRepository(this);
        this.addValidationContext(new UmbValidationContext(this));

        this.#entityContext.setEntityType(UAI_PROMPT_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "create",
                component: UaiPromptWorkspaceEditorElement,
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
                component: UaiPromptWorkspaceEditorElement,
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
    }

    /**
     * Creates a scaffold for a new prompt.
     */
    async scaffold() {
        this.resetState();
        const { data } = await this.#repository.createScaffold();
        if (data) {
            this.#unique.setValue(UAI_EMPTY_GUID);
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    /**
     * Loads an existing prompt by ID.
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
                        this.#model.setValue(structuredClone(model));
                        this.setIsNew(false);
                    }
                },
                "_observeModel"
            );
        }

        return data;
    }

    /**
     * Updates a property of the model.
     */
    updateProperty<K extends keyof UaiPromptDetailModel>(key: K, value: UaiPromptDetailModel[K]) {
        const currentValue = this.#model.getValue();
        if (currentValue) {
            this.#model.setValue({
                ...currentValue,
                [key]: value,
            });
        }
    }

    getData(): UaiPromptDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_PROMPT_ENTITY_TYPE;
    }

    /**
     * Saves the prompt (create or update).
     */
    async submit() {
        const model = this.#model.getValue();
        if (!model) return;

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

        this.setIsNew(false);
    }
}

export { UaiPromptWorkspaceContext as api };

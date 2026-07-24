import { UmbSubmittableWorkspaceContextBase } from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext } from "@umbraco-cms/backoffice/validation";
import { UaiCommandStore, type UaiCommand } from "@umbraco-ai/core";
import { UaiProjectRepository } from "../repository/project.repository.js";
import { UAI_PROJECT_WORKSPACE_ALIAS, UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";
import {
    createProjectScaffold,
    toProjectDetailModel,
    toProjectRequestModel,
    UAI_EMPTY_GUID,
    type UaiProjectDetailModel,
} from "../types.js";

/**
 * Workspace context for creating/editing a project. Mirrors the core Context workspace (submittable
 * base + command store + validation + entity context) but is driven by the section shell's router
 * (it exposes `scaffold()`/`load(id)` for the shell to call) rather than the CMS workspace
 * route-manager, and reuses the reactive `UaiProjectRepository` so a successful save also updates the
 * sidebar tree via the shared entity-action bus.
 */
export class UaiProjectWorkspaceContext extends UmbSubmittableWorkspaceContextBase<UaiProjectDetailModel> {
    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiProjectDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository = new UaiProjectRepository(this);
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);
    #validationContext: UmbValidationContext;

    /** Default project name, injected by the shell (localized) for the create scaffold. */
    defaultName = "Untitled project";

    get validation() {
        return this.#validationContext;
    }

    constructor(host: UmbControllerHost) {
        super(host, UAI_PROJECT_WORKSPACE_ALIAS);
        this.#validationContext = new UmbValidationContext(this);
        this.addValidationContext(this.#validationContext);

        this.#entityContext.setEntityType(UAI_PROJECT_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));
    }

    protected override resetState(): void {
        super.resetState();
        this.#unique.setValue(undefined);
        this.#model.setValue(undefined);
        this.#commandStore.reset();
    }

    /** Scaffolds a new (unsaved) project. */
    scaffold(): void {
        this.resetState();
        this.#unique.setValue(UAI_EMPTY_GUID);
        this.#model.setValue(createProjectScaffold(this.defaultName));
        this.setIsNew(true);
    }

    /** Loads an existing project by id. */
    async load(id: string): Promise<void> {
        this.resetState();
        const { data } = await this.#repository.requestById(id);
        if (!data) return;
        this.#unique.setValue(data.id);
        this.#model.setValue(toProjectDetailModel(data));
        this.setIsNew(false);
    }

    /** Applies a command to the model (mirrors the Context view's write path). */
    handleCommand(command: UaiCommand): void {
        const current = this.#model.getValue();
        if (!current) return;
        const next = structuredClone(current);
        command.execute(next);
        this.#model.setValue(next);
        this.#commandStore.add(command);
    }

    getData(): UaiProjectDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_PROJECT_ENTITY_TYPE;
    }

    async submit(): Promise<void> {
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
            const request = toProjectRequestModel(model);
            if (this.getIsNew()) {
                const { data, error } = await this.#repository.create(request);
                if (error) throw error;
                if (data) {
                    this.#unique.setValue(data.id);
                    this.#model.setValue(toProjectDetailModel(data));
                }
            } else {
                const { error } = await this.#repository.update(model.unique, request);
                if (error) throw error;
                // The current model already reflects the saved state (update returns no body).
            }
            this.#commandStore.reset();
            this.setIsNew(false);
        } finally {
            this.#commandStore.unmute();
        }
    }
}

export { UaiProjectWorkspaceContext as api };

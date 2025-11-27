import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UmbWorkspaceRouteManager, UmbSubmittableWorkspaceContextBase } from "@umbraco-cms/backoffice/workspace";
import { UmbContextToken } from "@umbraco-cms/backoffice/context-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UmbValidationContext } from "@umbraco-cms/backoffice/validation";
import { UaiConnectionDetailRepository } from "../repository/detail/connection-detail.repository.js";
import { UaiConnectionConstants } from "../constants.js";
import type { UaiConnectionDetailModel } from "../types.js";
import type { UaiCommand } from "../../core/command/command.base.js";
import { UaiCommandStore } from "../../core/command/command.store.js";

export const UAI_CONNECTION_WORKSPACE_CONTEXT = new UmbContextToken<UaiConnectionWorkspaceContext>(
    "UmbWorkspaceContext",
    undefined,
    (context): context is UaiConnectionWorkspaceContext =>
        context.getEntityType() === UaiConnectionConstants.EntityType.Entity
);

/**
 * Workspace context for editing Connection entities.
 * Handles CRUD operations, state management, and command tracking.
 */
export class UaiConnectionWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiConnectionDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiConnectionDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiConnectionDetailRepository;
    #commandStore = new UaiCommandStore();
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UaiConnectionConstants.Workspace.Entity);

        this.#repository = new UaiConnectionDetailRepository(this);
        this.addValidationContext(new UmbValidationContext(this));

        this.#entityContext.setEntityType(UaiConnectionConstants.EntityType.Entity);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));
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
        return UaiConnectionConstants.EntityType.Entity;
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
}

export { UaiConnectionWorkspaceContext as api };

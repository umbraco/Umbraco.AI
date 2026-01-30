import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import {
    UmbWorkspaceRouteManager,
    UmbSubmittableWorkspaceContextBase,
} from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UaiTestDetailRepository } from "../../repository/detail/test-detail.repository.js";
import { UAI_TEST_WORKSPACE_ALIAS, UAI_TEST_ENTITY_TYPE } from "../../constants.js";
import type { UaiTestDetailModel } from "../../types.js";

/**
 * Workspace context for editing Test entities.
 * Handles CRUD operations and state management.
 *
 * TODO: This is a basic implementation. Needs to be enhanced with:
 * - Route management for create/edit workflows
 * - Command tracking
 * - Validation
 * - Full workspace lifecycle
 */
export class UaiTestWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiTestDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiTestDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiTestDetailRepository;
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_TEST_WORKSPACE_ALIAS);

        this.#repository = new UaiTestDetailRepository(this);
        this.#entityContext.setEntityType(UAI_TEST_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        // TODO: Set up routes for create/edit
    }

    protected resetState(): void {
        super.resetState();
        this.#unique.setValue(undefined);
        this.#model.setValue(undefined);
    }

    async load(unique: string) {
        this.resetState();
        const { data } = await this.#repository.read(unique);
        if (data) {
            this.#unique.setValue(data.unique);
            this.#model.setValue(data);
        }
    }

    async scaffold() {
        this.resetState();
        const { data } = await this.#repository.createScaffold();
        if (data) {
            this.#model.setValue(data);
            this.setIsNew(true);
        }
    }

    getData() {
        return this.#model.getValue();
    }

    getUnique() {
        return this.#unique.getValue();
    }

    getEntityType() {
        return UAI_TEST_ENTITY_TYPE;
    }

    setName(name: string) {
        const current = this.#model.getValue();
        if (current) {
            this.#model.update({ ...current, name });
        }
    }

    async save() {
        const model = this.getData();
        if (!model) return;

        if (this.getIsNew()) {
            const { data } = await this.#repository.create(model);
            if (data) {
                this.setIsNew(false);
                this.#unique.setValue(data.unique);
                this.#model.setValue(data);
            }
        } else {
            await this.#repository.save(model);
        }
    }

    async delete() {
        const unique = this.getUnique();
        if (unique) {
            await this.#repository.delete(unique);
        }
    }

    public override destroy(): void {
        this.#repository.destroy();
        super.destroy();
    }
}

export { UaiTestWorkspaceContext as api };
export default UaiTestWorkspaceContext;

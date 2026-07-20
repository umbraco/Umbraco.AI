import type { UmbRoutableWorkspaceContext } from "@umbraco-cms/backoffice/workspace";
import { UmbWorkspaceRouteManager, UmbSubmittableWorkspaceContextBase } from "@umbraco-cms/backoffice/workspace";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UmbBasicState, UmbObjectState } from "@umbraco-cms/backoffice/observable-api";
import { UmbEntityContext } from "@umbraco-cms/backoffice/entity";
import { UaiKnowledgeSetDetailRepository } from "../../repository/detail/knowledge-set-detail.repository.js";
import { UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS } from "../constants.js";
import { UAI_KNOWLEDGE_SET_ENTITY_TYPE } from "../../entity.js";
import type { UaiKnowledgeSetDetailModel } from "../../types.js";
import { UaiKnowledgeSetWorkspaceEditorElement } from "./knowledge-set-workspace-editor.element.js";

/**
 * Read-only workspace context for auditing a single installed knowledge set.
 *
 * Knowledge sets are code-defined and auto-active, so this context only loads a set by id and exposes
 * its model. Unlike `UaiContextWorkspaceContext` there is no scaffold, no command tracking, and no
 * meaningful submit — `submit` is a no-op and no save action is registered.
 */
export class UaiKnowledgeSetWorkspaceContext
    extends UmbSubmittableWorkspaceContextBase<UaiKnowledgeSetDetailModel>
    implements UmbRoutableWorkspaceContext
{
    readonly routes = new UmbWorkspaceRouteManager(this);

    #unique = new UmbBasicState<string | undefined>(undefined);
    readonly unique = this.#unique.asObservable();

    #model = new UmbObjectState<UaiKnowledgeSetDetailModel | undefined>(undefined);
    readonly model = this.#model.asObservable();

    #repository: UaiKnowledgeSetDetailRepository;
    #entityContext = new UmbEntityContext(this);

    constructor(host: UmbControllerHost) {
        super(host, UAI_KNOWLEDGE_SET_WORKSPACE_ALIAS);

        this.#repository = new UaiKnowledgeSetDetailRepository(this);

        this.#entityContext.setEntityType(UAI_KNOWLEDGE_SET_ENTITY_TYPE);
        this.observe(this.unique, (unique) => this.#entityContext.setUnique(unique ?? null));

        this.routes.setRoutes([
            {
                path: "edit/:id",
                component: UaiKnowledgeSetWorkspaceEditorElement,
                setup: (_component, info) => {
                    this.load(info.match.params.id);
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
     * Loads a knowledge set by its id.
     */
    async load(id: string) {
        this.resetState();
        const { data } = await this.#repository.requestById(id);

        if (data) {
            this.#unique.setValue(data.unique);
            this.#model.setValue(data);
            this.setIsNew(false);
        }

        return data;
    }

    getData(): UaiKnowledgeSetDetailModel | undefined {
        return this.#model.getValue();
    }

    getUnique(): string | undefined {
        return this.#unique.getValue();
    }

    getEntityType(): string {
        return UAI_KNOWLEDGE_SET_ENTITY_TYPE;
    }

    /**
     * Knowledge sets are read-only — there is nothing to submit.
     */
    async submit(): Promise<void> {
        // Intentionally a no-op: no save action is registered for this workspace.
    }

    public destroy(): void {
        this.#model.destroy();
        this.#unique.destroy();
        super.destroy();
    }
}

export { UaiKnowledgeSetWorkspaceContext as api };

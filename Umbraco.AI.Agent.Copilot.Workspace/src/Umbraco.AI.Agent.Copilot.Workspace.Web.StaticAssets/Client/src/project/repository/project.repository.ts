import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { BehaviorSubject, type Observable } from "@umbraco-cms/backoffice/external/rxjs";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UaiEntityActionEvent, dispatchActionEvent } from "@umbraco-ai/core";
import { UaiProjectServerDataSource } from "./project.server.data-source.js";
import { UAI_PROJECT_ENTITY_TYPE } from "../../constants.js";
import type { ProjectRequestModel, ProjectResponseModel } from "../../api/types.gen.js";

/**
 * Reactive repository for projects. Holds an observable map of projects (`projectItems$`) that
 * re-emits whenever a project is created/updated/deleted — anywhere in the app — by listening to
 * `UaiEntityActionEvent`s on the shared `UMB_ACTION_EVENT_CONTEXT` bus and dispatching them from its
 * own mutations. Every consumer news up its own instance; the global event bus keeps them in sync,
 * so creating a project in the editor makes the sidebar tree update with no manual reload.
 *
 * Mirrors `UaiAgentRepository`'s reactive-list pattern.
 */
export class UaiProjectRepository extends UmbControllerBase {
    #source: UaiProjectServerDataSource;
    #projectItems$ = new BehaviorSubject<Map<string, ProjectResponseModel>>(new Map());
    #isInitialized = false;

    constructor(host: UmbControllerHost) {
        super(host);
        this.#source = new UaiProjectServerDataSource(host);

        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
            context?.addEventListener(UaiEntityActionEvent.CREATED, this.#onProjectChanged as EventListener);
            context?.addEventListener(UaiEntityActionEvent.UPDATED, this.#onProjectChanged as EventListener);
            context?.addEventListener(UaiEntityActionEvent.DELETED, this.#onProjectDeleted as EventListener);
        });
    }

    /** Observable of all projects, keyed by id. Re-emits on any create/update/delete. */
    get projectItems$(): Observable<Map<string, ProjectResponseModel>> {
        return this.#projectItems$.asObservable();
    }

    /** Loads all projects into the reactive map. Call once when the repository is first used. */
    async initialize(): Promise<void> {
        const { data } = await this.#source.getCollection();
        const items = new Map<string, ProjectResponseModel>();
        (data?.items ?? []).forEach((project) => items.set(project.id, project));
        this.#projectItems$.next(items);
        this.#isInitialized = true;
    }

    async requestCollection() {
        return this.#source.getCollection();
    }

    async requestById(id: string) {
        return this.#source.getById(id);
    }

    async create(request: ProjectRequestModel) {
        const result = await this.#source.create(request);
        if (!result.error && result.data?.id) {
            dispatchActionEvent(this, UaiEntityActionEvent.created(result.data.id, UAI_PROJECT_ENTITY_TYPE));
        }
        return result;
    }

    async update(id: string, request: ProjectRequestModel) {
        const result = await this.#source.update(id, request);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.updated(id, UAI_PROJECT_ENTITY_TYPE));
        }
        return result;
    }

    async delete(id: string) {
        const result = await this.#source.delete(id);
        if (!result.error) {
            dispatchActionEvent(this, UaiEntityActionEvent.deleted(id, UAI_PROJECT_ENTITY_TYPE));
        }
        return result;
    }

    #onProjectChanged = (event: UaiEntityActionEvent) => {
        if (!this.#isInitialized || event.getEntityType() !== UAI_PROJECT_ENTITY_TYPE) return;
        const unique = event.getUnique();
        if (unique) void this.#refreshEntry(unique);
    };

    #onProjectDeleted = (event: UaiEntityActionEvent) => {
        if (!this.#isInitialized || event.getEntityType() !== UAI_PROJECT_ENTITY_TYPE) return;
        const unique = event.getUnique();
        if (unique) this.#removeEntry(unique);
    };

    async #refreshEntry(id: string): Promise<void> {
        const { data, error } = await this.#source.getById(id);
        if (error || !data) {
            this.#removeEntry(id);
            return;
        }
        const current = new Map(this.#projectItems$.value);
        current.set(id, data);
        this.#projectItems$.next(current);
    }

    #removeEntry(id: string): void {
        const current = new Map(this.#projectItems$.value);
        if (current.delete(id)) {
            this.#projectItems$.next(current);
        }
    }
}

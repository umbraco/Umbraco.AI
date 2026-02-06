import { UmbControllerBase } from "@umbraco-cms/backoffice/class-api";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UaiEntityActionEvent } from "../events/entity-action.event.js";

/**
 * Configuration for the entity deleted redirect controller.
 * @public
 */
export interface UaiEntityDeletedRedirectArgs {
    /** Function to get the current entity's unique identifier */
    getUnique: () => string | undefined;
    /** Function to get the current entity's type */
    getEntityType: () => string | undefined;
    /** Path to redirect to after the entity is deleted */
    collectionPath: string;
}

/**
 * Controller that redirects to the collection view when the current entity is deleted.
 * Add this to workspace contexts that support deletion.
 * @public
 */
export class UaiEntityDeletedRedirectController extends UmbControllerBase {
    static readonly ALIAS = "UaiEntityDeletedRedirectController";

    #args: UaiEntityDeletedRedirectArgs;

    constructor(host: UmbControllerHost, args: UaiEntityDeletedRedirectArgs) {
        super(host, UaiEntityDeletedRedirectController.ALIAS);
        this.#args = args;

        this.consumeContext(UMB_ACTION_EVENT_CONTEXT, (context) => {
            context?.addEventListener(UaiEntityActionEvent.DELETED, this.#onEntityDeleted as EventListener);
        });
    }

    #onEntityDeleted = (event: UaiEntityActionEvent) => {
        const unique = event.getUnique();
        const entityType = event.getEntityType();
        const currentUnique = this.#args.getUnique();
        const currentEntityType = this.#args.getEntityType();

        if (unique === currentUnique && entityType === currentEntityType) {
            window.history.pushState({}, "", this.#args.collectionPath);
            window.dispatchEvent(new PopStateEvent("popstate"));
        }
    };
}

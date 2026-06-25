import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import {
    UmbRequestReloadChildrenOfEntityEvent,
    UmbRequestReloadStructureForEntityEvent,
} from "@umbraco-cms/backoffice/entity-action";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { umbPeekError } from "@umbraco-cms/backoffice/notification";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import { UMB_ENTITY_CONTEXT } from "@umbraco-cms/backoffice/entity";
import type { UmbDetailRepository } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

/**
 * Configuration for the bulk delete action.
 * @public
 */
export interface UaiBulkDeleteActionArgs {
    /** Localization key or text for the dialog headline */
    headline: string;
    /** Localization key for the confirmation message (will be interpolated with count) */
    confirmMessage: string;
    /** Factory function to create the detail repository */
    getRepository: (host: UmbControllerHost) => UmbDetailRepository<unknown>;
}

/**
 * Reusable bulk delete action for Umbraco.AI entities.
 * Extend this class and provide configuration via getArgs().
 *
 * Note: Event dispatching is handled by the repository for each deleted item.
 * @public
 */
export abstract class UaiBulkDeleteActionBase extends UmbEntityBulkActionBase<never> {
    #localize = new UmbLocalizationController(this);

    /**
     * Override this method to provide the bulk delete action configuration.
     */
    protected abstract getArgs(): UaiBulkDeleteActionArgs;

    async execute() {
        if (!this.selection || this.selection.length === 0) {
            throw new Error("No items selected.");
        }

        const { headline, confirmMessage, getRepository } = this.getArgs();

        await umbConfirmModal(this, {
            headline,
            content: this.#localize.string(confirmMessage, this.selection.length),
            color: "danger",
            confirmLabel: "#actions_delete",
        });

        const repository = getRepository(this);

        for (const unique of this.selection) {
            const { error } = await repository.delete(unique);
            if (error) {
                const problemDetails = error as { title?: string; detail?: string };
                await umbPeekError(this, {
                    headline: problemDetails.title,
                    message: problemDetails.detail ?? problemDetails.title ?? "An item could not be deleted.",
                });
            }
            // The repository dispatches the entity-deleted event per item.
        }

        // Request one reload of the parent's children and structure via the action event context
        // (as CMS's UmbDeleteEntityBulkAction does). Every collection, tree and structure consumer
        // refreshes once; the collection's action-executed handler clears the selection toolbar.
        const entityContext = await this.getContext(UMB_ENTITY_CONTEXT);
        const eventContext = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
        const entityType = entityContext?.getEntityType();
        const unique = entityContext?.getUnique();
        if (eventContext && entityType && unique !== undefined) {
            const args = { entityType, unique };
            eventContext.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent(args));
            eventContext.dispatchEvent(new UmbRequestReloadStructureForEntityEvent(args));
        }
    }
}

import { UmbEntityBulkActionBase } from "@umbraco-cms/backoffice/entity-bulk-action";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { umbPeekError } from "@umbraco-cms/backoffice/notification";
import { UmbLocalizationController } from "@umbraco-cms/backoffice/localization-api";
import type { UmbDetailRepository } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";

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

        // Refresh the collection once, after every delete has completed. The repository no longer
        // reloads per item: N items used to fire N uncancelled, racing reloads, so an early
        // (pre-delete) response could resolve last and leave deleted rows on screen. Clearing the
        // selection also dismisses the now-stale bulk-action selection toolbar.
        const collectionContext = await this.getContext(UMB_COLLECTION_CONTEXT);
        collectionContext?.selection.clearSelection();
        collectionContext?.loadCollection();
    }
}

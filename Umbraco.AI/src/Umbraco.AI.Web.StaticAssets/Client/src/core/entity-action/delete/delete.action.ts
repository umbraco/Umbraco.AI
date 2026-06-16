import { UmbEntityActionBase, UmbRequestReloadStructureForEntityEvent } from "@umbraco-cms/backoffice/entity-action";
import { umbConfirmModal } from "@umbraco-cms/backoffice/modal";
import { umbPeekError } from "@umbraco-cms/backoffice/notification";
import { UMB_ACTION_EVENT_CONTEXT } from "@umbraco-cms/backoffice/action";
import type { UmbDetailRepository } from "@umbraco-cms/backoffice/repository";
import type { UmbControllerHost } from "@umbraco-cms/backoffice/controller-api";

/**
 * Configuration for the delete action.
 * @public
 */
export interface UaiDeleteActionArgs {
    /** Localization key or text for the dialog headline */
    headline: string;
    /** Localization key or text for the confirmation message */
    confirmMessage: string;
    /** Factory function to create the detail repository */
    getRepository: (host: UmbControllerHost) => UmbDetailRepository<unknown>;
}

/**
 * Reusable delete action for Umbraco.AI entities.
 * Extend this class and provide configuration via getArgs().
 *
 * Note: Event dispatching is handled by the repository, not the action.
 * @public
 */
export abstract class UaiDeleteActionBase extends UmbEntityActionBase<never> {
    /**
     * Override this method to provide the delete action configuration.
     */
    protected abstract getArgs(): UaiDeleteActionArgs;

    async execute() {
        if (!this.args.unique) {
            throw new Error("Cannot delete without unique identifier.");
        }

        const { headline, confirmMessage, getRepository } = this.getArgs();

        await umbConfirmModal(this, {
            headline,
            content: confirmMessage,
            color: "danger",
            confirmLabel: "#actions_delete",
        });

        const repository = getRepository(this);
        const { error } = await repository.delete(this.args.unique);

        if (error) {
            const problemDetails = error as { title?: string; detail?: string };
            await umbPeekError(this, {
                headline: problemDetails.title,
                message: problemDetails.detail ?? problemDetails.title ?? "The item could not be deleted.",
            });
            throw error;
        }

        // Request a reload for this entity via the action event context (as CMS's
        // UmbDeleteEntityAction does). Any collection, tree or structure consumer listening for
        // the entity refreshes itself.
        const eventContext = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
        eventContext?.dispatchEvent(
            new UmbRequestReloadStructureForEntityEvent({
                unique: this.args.unique,
                entityType: this.args.entityType,
            }),
        );
    }
}

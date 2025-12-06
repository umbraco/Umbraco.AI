import { UmbEntityBulkActionBase } from '@umbraco-cms/backoffice/entity-bulk-action';
import { umbConfirmModal } from '@umbraco-cms/backoffice/modal';
import { UmbLocalizationController } from '@umbraco-cms/backoffice/localization-api';
import type { UmbDetailRepository } from '@umbraco-cms/backoffice/repository';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';

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
 * Reusable bulk delete action for Umbraco.Ai entities.
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
            throw new Error('No items selected.');
        }

        const { headline, confirmMessage, getRepository } = this.getArgs();

        await umbConfirmModal(this, {
            headline,
            content: this.#localize.string(confirmMessage, this.selection.length),
            color: 'danger',
            confirmLabel: '#actions_delete',
        });

        const repository = getRepository(this);

        for (const unique of this.selection) {
            await repository.delete(unique);
            // Event is dispatched by the repository for each successful delete
        }
    }
}

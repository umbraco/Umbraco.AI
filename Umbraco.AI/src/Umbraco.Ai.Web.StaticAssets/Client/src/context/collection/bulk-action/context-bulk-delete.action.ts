import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from '../../../core/entity-bulk-action/delete/bulk-delete.action.js';
import { UaiContextDetailRepository } from '../../repository/detail/context-detail.repository.js';

export class UaiContextBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: '#actions_delete',
            confirmMessage: '#uaiContext_bulkDeleteConfirm',
            getRepository: (host) => new UaiContextDetailRepository(host),
        };
    }
}

export { UaiContextBulkDeleteAction as api };

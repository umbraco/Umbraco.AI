import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from '../../../core/entity-bulk-action/delete/uai-bulk-delete.action.js';
import { UaiConnectionDetailRepository } from '../../repository/detail/connection-detail.repository.js';

export class UaiConnectionBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: '#actions_delete',
            getConfirmMessage: (count) => `Are you sure you want to delete ${count} connection(s)? This cannot be undone.`,
            getRepository: (host) => new UaiConnectionDetailRepository(host),
        };
    }
}

export { UaiConnectionBulkDeleteAction as api };

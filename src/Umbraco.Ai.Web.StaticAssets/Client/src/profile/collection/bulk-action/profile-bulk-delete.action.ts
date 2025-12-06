import { UaiBulkDeleteActionBase, type UaiBulkDeleteActionArgs } from '../../../core/entity-bulk-action/delete/uai-bulk-delete.action.js';
import { UaiProfileDetailRepository } from '../../repository/detail/profile-detail.repository.js';

export class UaiProfileBulkDeleteAction extends UaiBulkDeleteActionBase {
    protected getArgs(): UaiBulkDeleteActionArgs {
        return {
            headline: '#actions_delete',
            getConfirmMessage: (count) => `Are you sure you want to delete ${count} profile(s)? This cannot be undone.`,
            getRepository: (host) => new UaiProfileDetailRepository(host),
        };
    }
}

export { UaiProfileBulkDeleteAction as api };

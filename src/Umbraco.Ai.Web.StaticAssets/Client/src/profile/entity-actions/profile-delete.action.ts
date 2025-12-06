import { UaiDeleteActionBase, type UaiDeleteActionArgs } from '../../core/entity-action/delete/delete.action.js';
import { UaiProfileDetailRepository } from '../repository/detail/profile-detail.repository.js';

export class UaiProfileDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: '#actions_delete',
            confirmMessage: '#uaiProfile_deleteConfirm',
            getRepository: (host) => new UaiProfileDetailRepository(host),
        };
    }
}

export { UaiProfileDeleteAction as api };

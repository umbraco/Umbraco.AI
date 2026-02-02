import { UaiDeleteActionBase, type UaiDeleteActionArgs } from '../../core/entity-action/delete/delete.action.js';
import { UaiConnectionDetailRepository } from '../repository/detail/connection-detail.repository.js';

export class UaiConnectionDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: '#actions_delete',
            confirmMessage: '#uaiConnection_deleteConfirm',
            getRepository: (host) => new UaiConnectionDetailRepository(host),
        };
    }
}

export { UaiConnectionDeleteAction as api };

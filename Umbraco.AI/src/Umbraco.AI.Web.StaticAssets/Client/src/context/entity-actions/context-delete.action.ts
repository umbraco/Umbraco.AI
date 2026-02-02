import { UaiDeleteActionBase, type UaiDeleteActionArgs } from '../../core/entity-action/delete/delete.action.js';
import { UaiContextDetailRepository } from '../repository/detail/context-detail.repository.js';

export class UaiContextDeleteAction extends UaiDeleteActionBase {
    protected getArgs(): UaiDeleteActionArgs {
        return {
            headline: '#actions_delete',
            confirmMessage: '#uaiContext_deleteConfirm',
            getRepository: (host) => new UaiContextDetailRepository(host),
        };
    }
}

export { UaiContextDeleteAction as api };

import { UmbEntityActionEvent } from '@umbraco-cms/backoffice/entity-action';

/**
 * Custom event for Umbraco.AI entity actions.
 * Dispatched by repositories after successful CRUD operations.
 * @public
 */
export class UaiEntityActionEvent extends UmbEntityActionEvent {
    static readonly CREATED = 'uai-entity-created';
    static readonly UPDATED = 'uai-entity-updated';
    static readonly DELETED = 'uai-entity-deleted';

    static created(unique: string, entityType: string) {
        return new UaiEntityActionEvent(UaiEntityActionEvent.CREATED, unique, entityType);
    }

    static updated(unique: string, entityType: string) {
        return new UaiEntityActionEvent(UaiEntityActionEvent.UPDATED, unique, entityType);
    }

    static deleted(unique: string, entityType: string) {
        return new UaiEntityActionEvent(UaiEntityActionEvent.DELETED, unique, entityType);
    }

    constructor(type: string, unique: string, entityType: string) {
        super(type, { unique, entityType });
    }
}

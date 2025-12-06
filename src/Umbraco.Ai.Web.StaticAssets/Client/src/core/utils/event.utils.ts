import type { UmbClassInterface } from '@umbraco-cms/backoffice/class-api';
import { UMB_ACTION_EVENT_CONTEXT } from '@umbraco-cms/backoffice/action';

/**
 * Dispatches an event through the action event context.
 * @public
 */
export function dispatchActionEvent(host: UmbClassInterface, event: Event) {
    host.getContext(UMB_ACTION_EVENT_CONTEXT).then((context) => {
        context?.dispatchEvent(event);
    });
}

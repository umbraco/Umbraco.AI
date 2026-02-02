import { UMB_ACTION_EVENT_CONTEXT, type UmbActionEventContext } from '@umbraco-cms/backoffice/action';

/**
 * Dispatches an event through the action event context.
 *
 * Note: The host parameter uses `any` to avoid cross-package type coupling.
 * When consumers import from different @umbraco-cms/backoffice instances,
 * strict typing on UmbClassInterface causes TypeScript errors even with identical versions.
 * @public
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function dispatchActionEvent(host: any, event: Event) {
    (host.getContext(UMB_ACTION_EVENT_CONTEXT) as Promise<UmbActionEventContext | undefined>).then((context) => {
        context?.dispatchEvent(event);
    });
}

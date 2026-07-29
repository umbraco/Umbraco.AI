import { UMB_ACTION_EVENT_CONTEXT, type UmbActionEventContext } from "@umbraco-cms/backoffice/action";

/**
 * Dispatches an event through the action event context.
 *
 * The context is resolved asynchronously, so the dispatch lands a tick after this is called. Callers that
 * guard against reacting to their own write (a "self write" counter released when the request settles) must
 * await the returned promise before releasing that guard, or the event arrives after the guard is gone and
 * they refetch what they just wrote. Fire-and-forget callers can ignore it.
 *
 * Note: The host parameter uses `any` to avoid cross-package type coupling.
 * When consumers import from different @umbraco-cms/backoffice instances,
 * strict typing on UmbClassInterface causes TypeScript errors even with identical versions.
 * @public
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function dispatchActionEvent(host: any, event: Event): Promise<void> {
    return (host.getContext(UMB_ACTION_EVENT_CONTEXT) as Promise<UmbActionEventContext | undefined>).then(
        (context) => {
            context?.dispatchEvent(event);
        },
    );
}

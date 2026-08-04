import { Observable, shareReplay } from "@umbraco-cms/backoffice/external/rxjs";

/**
 * Wraps a boolean source so the rising edge (`false` -> `true`) passes through immediately, but the
 * falling edge (`true` -> `false`) is delayed by `hideDelayMs`. A pending hide is cancelled if the
 * source returns to `true` within the window.
 *
 * The copilot uses this on its "is this a supported workspace" signal: moving between two supported
 * workspaces briefly empties the detected-entity list as one workspace tears down before the next
 * registers. Without the delay that transient gap would flip the signal to `false`, flickering the
 * FAB out and in and auto-closing the sidebar (which resets the conversation). Showing immediately
 * keeps the UI responsive when a workspace becomes supported.
 *
 * The result is `shareReplay(1)` multicast so multiple consumers (the FAB and the sidebar) share a
 * single timer and the same emissions, and late subscribers get the current value.
 *
 * @param source$ The instantaneous boolean signal.
 * @param hideDelayMs Milliseconds to hold a `true` before allowing the flip to `false`.
 */
export function debouncedHide(source$: Observable<boolean>, hideDelayMs: number): Observable<boolean> {
    return new Observable<boolean>((subscriber) => {
        let hideTimer = 0;
        let current: boolean | undefined;
        const emit = (value: boolean) => {
            if (current !== value) {
                current = value;
                subscriber.next(value);
            }
        };
        const sub = source$.subscribe((on) => {
            window.clearTimeout(hideTimer);
            if (on) {
                emit(true);
            } else {
                hideTimer = window.setTimeout(() => emit(false), hideDelayMs);
            }
        });
        return () => {
            window.clearTimeout(hideTimer);
            sub.unsubscribe();
        };
    }).pipe(shareReplay(1));
}

/**
 * Section Detection Helper
 *
 * Provides URL-based section detection for the copilot via reactive observables.
 *
 * WORKAROUND: This exists because the built-in Umb.Condition.SectionAlias does not work
 * for headerApp extensions. Header apps are rendered outside the section context, so
 * UMB_SECTION_CONTEXT is not available. UMB_BACKOFFICE_CONTEXT has activeSectionAlias
 * but is not publicly exported.
 *
 * TODO: Remove this file and use context-based detection when the upstream issue is resolved.
 * See: https://github.com/umbraco/Umbraco-CMS/issues/21486
 *
 * When fixed, update:
 * - copilot-section.condition.ts: Use Umb.Condition.SectionAlias or UMB_BACKOFFICE_CONTEXT
 * - copilot-sidebar.element.ts: Use context-based section detection
 * - copilot-agent.repository.ts: Use context-based section detection
 * - Delete this file (section-detector.ts)
 */

/**
 * Extracts the section pathname from the current URL.
 * Umbraco URLs follow the pattern: /section/{pathname}/...
 */
export function getSectionPathnameFromUrl(): string | null {
    const path = window.location.pathname;
    const match = path.match(/\/section\/([^/]+)/);
    return match ? match[1] : null;
}

/**
 * Checks if a section pathname is in the allowed list.
 */
export function isSectionAllowed(pathname: string | null, allowedPathnames: string[]): boolean {
    return pathname ? allowedPathnames.includes(pathname) : false;
}

import { Observable, shareReplay } from "@umbraco-cms/backoffice/external/rxjs";

/**
 * Module-level shared observable instance.
 * Ensures history API is only monkey-patched once, regardless of subscriber count.
 */
let _sharedSectionObservable$: Observable<string | null> | null = null;

/**
 * Stores original history methods to restore on teardown.
 */
let _originalPushState: typeof history.pushState | null = null;
let _originalReplaceState: typeof history.replaceState | null = null;
let _onPopState: (() => void) | null = null;

/**
 * Reference counter to track active subscriptions.
 */
let _refCount = 0;

/**
 * Creates an observable that emits the current section whenever navigation occurs.
 * Listens to browser navigation events (popstate, pushState, replaceState) instead of polling.
 *
 * Uses a shared observable to prevent history API corruption when multiple subscribers exist.
 * The history methods are only patched once and restored when all subscribers unsubscribe.
 *
 * @returns Observable<string | null> that emits section pathname on navigation
 */
export function createSectionObservable(): Observable<string | null> {
    if (!_sharedSectionObservable$) {
        _sharedSectionObservable$ = new Observable<string | null>((subscriber) => {
            // Increment ref count
            _refCount++;

            // Only patch history API on first subscription
            if (_refCount === 1) {
                // Emit initial value
                subscriber.next(getSectionPathnameFromUrl());

                // Listen to browser back/forward navigation
                _onPopState = () => {
                    subscriber.next(getSectionPathnameFromUrl());
                };

                // Intercept pushState and replaceState for SPA navigation
                _originalPushState = history.pushState;
                _originalReplaceState = history.replaceState;

                const wrappedPushState = function (this: History, ...args: Parameters<typeof history.pushState>) {
                    _originalPushState!.apply(this, args);
                    subscriber.next(getSectionPathnameFromUrl());
                };

                const wrappedReplaceState = function (this: History, ...args: Parameters<typeof history.replaceState>) {
                    _originalReplaceState!.apply(this, args);
                    subscriber.next(getSectionPathnameFromUrl());
                };

                // Install event listeners and method wrappers
                window.addEventListener("popstate", _onPopState);
                history.pushState = wrappedPushState;
                history.replaceState = wrappedReplaceState;
            } else {
                // For subsequent subscribers, just emit current value
                subscriber.next(getSectionPathnameFromUrl());
            }

            // Cleanup on unsubscribe
            return () => {
                _refCount--;

                // Only restore history API when last subscriber unsubscribes
                if (_refCount === 0) {
                    if (_onPopState) {
                        window.removeEventListener("popstate", _onPopState);
                        _onPopState = null;
                    }
                    if (_originalPushState) {
                        history.pushState = _originalPushState;
                        _originalPushState = null;
                    }
                    if (_originalReplaceState) {
                        history.replaceState = _originalReplaceState;
                        _originalReplaceState = null;
                    }

                    // Reset shared observable
                    _sharedSectionObservable$ = null;
                }
            };
        }).pipe(shareReplay(1));
    }

    return _sharedSectionObservable$;
}

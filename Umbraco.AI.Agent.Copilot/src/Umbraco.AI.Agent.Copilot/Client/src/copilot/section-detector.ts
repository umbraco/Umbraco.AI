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

import { Observable } from "@umbraco-cms/backoffice/external/rxjs";


/**
 * Creates an observable that emits the current section whenever navigation occurs.
 * Listens to browser navigation events (popstate, pushState, replaceState) instead of polling.
 *
 * @returns Observable<string | null> that emits section pathname on navigation
 */
export function createSectionObservable(): Observable<string | null> {
    return new Observable((subscriber) => {
        // Emit initial value
        subscriber.next(getSectionPathnameFromUrl());

        // Listen to browser back/forward navigation
        const onPopState = () => {
            subscriber.next(getSectionPathnameFromUrl());
        };

        // Intercept pushState and replaceState for SPA navigation
        const originalPushState = history.pushState;
        const originalReplaceState = history.replaceState;

        const wrappedPushState = function (this: History, ...args: Parameters<typeof history.pushState>) {
            originalPushState.apply(this, args);
            subscriber.next(getSectionPathnameFromUrl());
        };

        const wrappedReplaceState = function (this: History, ...args: Parameters<typeof history.replaceState>) {
            originalReplaceState.apply(this, args);
            subscriber.next(getSectionPathnameFromUrl());
        };

        // Install event listeners and method wrappers
        window.addEventListener("popstate", onPopState);
        history.pushState = wrappedPushState;
        history.replaceState = wrappedReplaceState;

        // Cleanup on unsubscribe
        return () => {
            window.removeEventListener("popstate", onPopState);
            history.pushState = originalPushState;
            history.replaceState = originalReplaceState;
        };
    });
}

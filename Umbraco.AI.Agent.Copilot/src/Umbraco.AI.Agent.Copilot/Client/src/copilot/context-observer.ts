/**
 * Context Observer
 *
 * Provides reactive observables for backoffice navigation context.
 * Monitors URL changes via browser navigation events (no polling).
 *
 * Supports both section-only detection (backward compatible) and
 * full context detection (section + workspace) for advanced features.
 */

import { Observable, shareReplay, map, distinctUntilChanged } from "@umbraco-cms/backoffice/external/rxjs";

export interface ContextInfo {
	section: string | null;
	workspace: string | null;
}

/**
 * Extracts section and workspace from the current URL.
 * Pattern: /section/{section}/workspace/{workspace}/...
 */
function getContextFromUrl(): ContextInfo {
	const path = window.location.pathname;
	const sectionMatch = path.match(/\/section\/([^/]+)/);
	const workspaceMatch = path.match(/\/workspace\/([^/]+)/);

	return {
		section: sectionMatch?.[1] ?? null,
		workspace: workspaceMatch?.[1] ?? null,
	};
}

// Module-level shared observable with refcounting
let _sharedContextObservable$: Observable<ContextInfo> | null = null;
let _originalPushState: typeof history.pushState | null = null;
let _originalReplaceState: typeof history.replaceState | null = null;
let _onPopState: (() => void) | null = null;
let _refCount = 0;

/**
 * Internal shared implementation.
 * Only patches history API once, regardless of subscriber count.
 */
function createSharedContextObservable(): Observable<ContextInfo> {
	if (!_sharedContextObservable$) {
		_sharedContextObservable$ = new Observable<ContextInfo>((subscriber) => {
			_refCount++;

			if (_refCount === 1) {
				// Emit initial value
				subscriber.next(getContextFromUrl());

				// Listen to browser back/forward navigation
				_onPopState = () => {
					subscriber.next(getContextFromUrl());
				};

				// Intercept pushState and replaceState for SPA navigation
				_originalPushState = history.pushState;
				_originalReplaceState = history.replaceState;

				const wrappedPushState = function (this: History, ...args: Parameters<typeof history.pushState>) {
					_originalPushState!.apply(this, args);
					subscriber.next(getContextFromUrl());
				};

				const wrappedReplaceState = function (this: History, ...args: Parameters<typeof history.replaceState>) {
					_originalReplaceState!.apply(this, args);
					subscriber.next(getContextFromUrl());
				};

				window.addEventListener("popstate", _onPopState);
				history.pushState = wrappedPushState;
				history.replaceState = wrappedReplaceState;
			} else {
				// For subsequent subscribers, just emit current value
				subscriber.next(getContextFromUrl());
			}

			// Cleanup on unsubscribe
			return () => {
				_refCount--;

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

					_sharedContextObservable$ = null;
				}
			};
		}).pipe(shareReplay(1));
	}

	return _sharedContextObservable$;
}

/**
 * Creates an observable that emits section changes.
 * Backward compatible API for existing consumers.
 */
export function createSectionObservable(): Observable<string | null> {
	return createSharedContextObservable().pipe(
		map((ctx) => ctx.section),
		distinctUntilChanged(),
	);
}

/**
 * Creates an observable that emits full context (section + workspace).
 * Use this for thread persistence and context-aware features.
 */
export function createContextObservable(): Observable<ContextInfo> {
	return createSharedContextObservable();
}

/**
 * Checks if a section pathname is in the allowed list.
 */
export function isSectionAllowed(pathname: string | null, allowedPathnames: string[]): boolean {
	return pathname ? allowedPathnames.includes(pathname) : false;
}

/**
 * Context Observer
 *
 * Provides a reactive observable of the current backoffice section, derived from
 * the URL. Monitors changes via browser navigation events (no polling).
 *
 * Used by the copilot agent repository to filter agents by section scope.
 */

import { Observable, shareReplay, distinctUntilChanged } from "@umbraco-cms/backoffice/external/rxjs";

/**
 * Extracts the section pathname from the current URL.
 * Pattern: /section/{section}/...
 */
function getSectionFromUrl(): string | null {
	const match = window.location.pathname.match(/\/section\/([^/]+)/);
	return match?.[1] ?? null;
}

// Module-level shared observable with refcounting
let _sharedSectionObservable$: Observable<string | null> | null = null;
let _originalPushState: typeof history.pushState | null = null;
let _originalReplaceState: typeof history.replaceState | null = null;
let _onPopState: (() => void) | null = null;
let _refCount = 0;

/**
 * Internal shared implementation.
 * Only patches the history API once, regardless of subscriber count.
 */
function createSharedSectionObservable(): Observable<string | null> {
	if (!_sharedSectionObservable$) {
		_sharedSectionObservable$ = new Observable<string | null>((subscriber) => {
			_refCount++;

			if (_refCount === 1) {
				// Emit initial value
				subscriber.next(getSectionFromUrl());

				// Listen to browser back/forward navigation
				_onPopState = () => {
					subscriber.next(getSectionFromUrl());
				};

				// Intercept pushState and replaceState for SPA navigation
				_originalPushState = history.pushState;
				_originalReplaceState = history.replaceState;

				const wrappedPushState = function (this: History, ...args: Parameters<typeof history.pushState>) {
					_originalPushState!.apply(this, args);
					subscriber.next(getSectionFromUrl());
				};

				const wrappedReplaceState = function (this: History, ...args: Parameters<typeof history.replaceState>) {
					_originalReplaceState!.apply(this, args);
					subscriber.next(getSectionFromUrl());
				};

				window.addEventListener("popstate", _onPopState);
				history.pushState = wrappedPushState;
				history.replaceState = wrappedReplaceState;
			} else {
				// For subsequent subscribers, just emit current value
				subscriber.next(getSectionFromUrl());
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

					_sharedSectionObservable$ = null;
				}
			};
		}).pipe(shareReplay(1));
	}

	return _sharedSectionObservable$;
}

/**
 * Creates an observable that emits the current section pathname, and again
 * whenever it changes.
 */
export function createSectionObservable(): Observable<string | null> {
	return createSharedSectionObservable().pipe(distinctUntilChanged());
}

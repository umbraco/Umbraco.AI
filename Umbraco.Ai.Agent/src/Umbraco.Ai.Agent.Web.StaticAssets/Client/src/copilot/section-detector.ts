/**
 * Section Detection Helper
 *
 * Provides URL-based section detection for the copilot.
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
 * - copilot-sidebar.element.ts: Use context instead of observeSectionChanges()
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

/**
 * Callback type for section change notifications.
 */
export type SectionChangeCallback = (pathname: string | null) => void;

/**
 * Creates a section change observer that notifies when the URL section changes.
 * Returns a cleanup function to stop observing.
 *
 * @param callback - Called whenever the section changes
 * @param pollInterval - How often to check for URL changes (ms). Default: 100
 * @returns Cleanup function to stop observing
 */
export function observeSectionChanges(
  callback: SectionChangeCallback,
  pollInterval = 100
): () => void {
  let lastUrl = window.location.href;
  let lastPathname = getSectionPathnameFromUrl();

  const checkAndNotify = () => {
    const currentPathname = getSectionPathnameFromUrl();
    if (currentPathname !== lastPathname) {
      lastPathname = currentPathname;
      callback(currentPathname);
    }
  };

  const handleNavigation = () => {
    if (window.location.href !== lastUrl) {
      lastUrl = window.location.href;
      checkAndNotify();
    }
  };

  // Listen for navigation events
  window.addEventListener("popstate", handleNavigation);
  window.addEventListener("navigated", handleNavigation);

  // Poll for URL changes as a fallback (some SPA routers don't fire events)
  const intervalId = setInterval(() => {
    if (window.location.href !== lastUrl) {
      lastUrl = window.location.href;
      checkAndNotify();
    }
  }, pollInterval);

  // Notify immediately with current section
  callback(lastPathname);

  // Return cleanup function
  return () => {
    window.removeEventListener("popstate", handleNavigation);
    window.removeEventListener("navigated", handleNavigation);
    clearInterval(intervalId);
  };
}

/**
 * Workspace Registry Types
 */

import type { Observable } from "@umbraco-cms/backoffice/external/rxjs";

/**
 * Represents a registered workspace context
 */
export interface WorkspaceEntry {
	/** The workspace context instance */
	context: object;
	/** The manifest alias (e.g., "Umb.Workspace.Document") */
	alias: string;
	/** Entity type (e.g., "document", "media", "block") */
	entityType: string | undefined;
	/** Entity unique ID (GUID) */
	entityUnique: string | undefined;
}

/**
 * Event emitted when workspace registration changes
 */
export interface WorkspaceChangeEvent {
	type: "added" | "removed" | "updated";
	key: string;
	entry: WorkspaceEntry;
}

/**
 * Internal interface for workspace context type checking
 */
export interface WorkspaceContextLike {
	getEntityType?(): string | undefined;
	getHostElement?(): Element | undefined;
	destroy?(): void;
	unique?: Observable<string | null | undefined>;
	_host?: Element;
	host?: Element;
}

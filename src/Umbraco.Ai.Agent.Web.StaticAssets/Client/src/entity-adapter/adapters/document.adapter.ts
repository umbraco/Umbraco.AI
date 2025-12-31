/**
 * Document Entity Adapter
 *
 * Handles serialization of Umbraco document entities for LLM context.
 * Currently supports TextBox and TextArea property editors only.
 */

import { map, type Observable } from "@umbraco-cms/backoffice/external/rxjs";
import type {
	UaiEntityAdapterApi,
	UaiEntityContext,
	UaiSerializedEntity,
	UaiSerializedProperty,
} from "../types.js";

// Supported text-based property editors for initial implementation
const SUPPORTED_EDITOR_ALIASES = ["Umbraco.TextBox", "Umbraco.TextArea"];

/**
 * Interface matching the essential methods of UmbDocumentWorkspaceContext.
 * We use duck-typing rather than importing the actual type to avoid tight coupling.
 */
interface DocumentWorkspaceContextLike {
	getEntityType(): string;
	getUnique(): string | undefined;
	getName(variantId?: unknown): string | undefined;
	getContentTypeUnique(): string | undefined;
	getValues(): Array<{
		alias: string;
		value?: unknown;
		culture: string | null;
		segment: string | null;
		editorAlias: string;
	}> | undefined;
	// name() is a METHOD that returns Observable<string> (not a property!)
	name?(variantId?: unknown): Observable<string>;
	// variants observable - contains name for each variant
	variants?: Observable<Array<{ name?: string; culture?: string | null }>>;
	// Structure manager for content type info including icon
	structure?: {
		ownerContentType?: Observable<{ icon?: string } | undefined>;
	};
}

/**
 * Adapter for Umbraco document entities.
 */
export class UaiDocumentAdapter implements UaiEntityAdapterApi {
	readonly entityType = "document";

	/**
	 * Check if the workspace context is a document workspace.
	 * Uses duck-typing to check for document-specific methods.
	 */
	canHandle(workspaceContext: unknown): boolean {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;
		return (
			typeof ctx?.getEntityType === "function" &&
			ctx.getEntityType() === "document" &&
			typeof ctx?.getUnique === "function" &&
			typeof ctx?.getName === "function"
		);
	}

	/**
	 * Extract entity identity from document workspace context.
	 */
	extractEntityContext(workspaceContext: unknown): UaiEntityContext {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;
		return {
			entityType: "document",
			unique: ctx.getUnique() ?? null,
		};
	}

	/**
	 * Get the current display name for the document.
	 */
	getName(workspaceContext: unknown): string {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;
		return ctx.getName() ?? "Untitled";
	}

	/**
	 * Get an observable for the document name for reactive updates.
	 * Uses the variants observable which properly tracks name changes.
	 */
	getNameObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;

		// Use variants observable - this properly tracks name changes
		// The variants array contains all variant data including names
		if (ctx.variants) {
			return ctx.variants.pipe(
				map((variants) => {
					// For invariant documents, there's one variant with culture: null
					// For variant documents, pick the first one (or we could expose selection later)
					const invariantVariant = variants.find((v) => v.culture === null);
					return invariantVariant?.name ?? variants[0]?.name;
				}),
			);
		}

		// Fallback: try name() method directly
		if (typeof ctx.name === "function") {
			try {
				return ctx.name();
			} catch {
				return undefined;
			}
		}

		return undefined;
	}

	/**
	 * Get the icon for the document from its content type.
	 * Returns undefined initially - use getIconObservable for reactive updates.
	 */
	getIcon(workspaceContext: unknown): string | undefined {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;
		// We can't synchronously get the icon since it comes from the structure manager
		// which is async. Return undefined and let the observable handle it.
		// Could also try to access a sync getter if available:
		const structure = ctx.structure as { getOwnerContentType?: () => { icon?: string } | undefined };
		if (typeof structure?.getOwnerContentType === "function") {
			return structure.getOwnerContentType()?.icon;
		}
		return undefined;
	}

	/**
	 * Get an observable for the document icon for reactive updates.
	 * Uses the structure manager's ownerContentType observable to get the icon.
	 */
	getIconObservable(workspaceContext: unknown): Observable<string | undefined> | undefined {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;

		// Get icon from content type structure manager
		if (ctx.structure?.ownerContentType) {
			return ctx.structure.ownerContentType.pipe(
				map((ct: { icon?: string } | undefined) => ct?.icon),
			);
		}

		return undefined;
	}

	/**
	 * Serialize document for LLM context.
	 * Only includes TextBox and TextArea properties for now.
	 */
	async serializeForLlm(workspaceContext: unknown): Promise<UaiSerializedEntity> {
		const ctx = workspaceContext as DocumentWorkspaceContextLike;

		const unique = ctx.getUnique();
		const name = ctx.getName() ?? "Untitled";
		const contentType = ctx.getContentTypeUnique();
		const values = ctx.getValues() ?? [];

		// Filter to supported text editors and serialize
		const properties: UaiSerializedProperty[] = values
			.filter((v) => SUPPORTED_EDITOR_ALIASES.includes(v.editorAlias))
			.map((v) => ({
				alias: v.alias,
				label: v.alias, // TODO: Get proper label from content type structure
				editorAlias: v.editorAlias,
				value: v.value,
			}));

		return {
			entityType: "document",
			unique: unique ?? "new",
			name,
			contentType: contentType ?? undefined,
			properties,
		};
	}
}

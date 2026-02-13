import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestWorkspace } from "@umbraco-cms/backoffice/workspace";
import "@umbraco-ai/core";
import type { UaiTagItem, UaiTagLookupCallback } from "@umbraco-ai/core";

/**
 * A tags input component for selecting entity type aliases.
 * Wraps uai-tags-input with automatic lookup from workspace manifests in the extensions registry.
 *
 * @fires change - Fires when tags are added or removed
 */
@customElement("uai-entity-type-tags-input")
export class UaiEntityTypeTagsInputElement extends UmbLitElement {
	/**
	 * The selected entity type aliases.
	 */
	@property({ type: Array })
	public set items(value: string[]) {
		this.#items = value ?? [];
	}
	public get items(): string[] {
		return this.#items;
	}
	#items: string[] = [];

	/**
	 * Placeholder text for the input.
	 */
	@property({ type: String })
	placeholder = "Select entity types";

	/**
	 * Whether the input is read-only.
	 */
	@property({ type: Boolean })
	readonly = false;

	/**
	 * Lookup callback for fetching entity types from workspace manifests.
	 */
	#lookup: UaiTagLookupCallback = async (query: string): Promise<UaiTagItem[]> => {
		// Get all workspace manifests
		const allExtensions = umbExtensionsRegistry.getByType("workspace");
		const entityTypes = new Set<string>();

		// Extract unique entity types from workspace meta
		allExtensions.forEach((ext) => {
			// Type guard: ensure it's a workspace with meta.entityType
			if (ext.type === "workspace" && "meta" in ext) {
				const workspace = ext as ManifestWorkspace;
				if (workspace.meta?.entityType) {
					entityTypes.add(workspace.meta.entityType);
				}
			}
		});

		const lowerQuery = query.toLowerCase();
		return Array.from(entityTypes)
			.filter((type) => {
				// Filter out -root entity types (e.g., "uai:profile-root")
				if (type.endsWith("-root")) return false;
				// Match query
				return type.toLowerCase().includes(lowerQuery);
			})
			.filter((type) => !this.#items.includes(type))
			.map((type) => ({
				id: type,
				text: type,
			}));
	};

	#onChange(event: Event) {
		event.stopPropagation();
		const target = event.target as HTMLElement & { items: string[] };
		this.#items = target.items;
		this.dispatchEvent(new UmbChangeEvent());
	}

	render() {
		return html`
			<uai-tags-input
				.items=${this.#items}
				.lookup=${this.#lookup}
				.placeholder=${this.placeholder}
				?readonly=${this.readonly}
				@change=${this.#onChange}
				strict
			></uai-tags-input>
		`;
	}

	static styles = [
		css`
			:host {
				display: block;
			}
		`,
	];
}

export default UaiEntityTypeTagsInputElement;

declare global {
	interface HTMLElementTagNameMap {
		"uai-entity-type-tags-input": UaiEntityTypeTagsInputElement;
	}
}

import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import "@umbraco-ai/core";
import type { UaiTagItem, UaiTagLookupCallback } from "@umbraco-ai/core";

/**
 * A tags input component for selecting section pathnames.
 * Wraps uai-tags-input with automatic lookup from the Umbraco extensions registry.
 *
 * @fires change - Fires when tags are added or removed
 */
@customElement("uai-section-tags-input")
export class UaiSectionTagsInputElement extends UmbLitElement {
	/**
	 * The selected section pathnames.
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
	placeholder = "Select sections";

	/**
	 * Whether the input is read-only.
	 */
	@property({ type: Boolean })
	readonly = false;

	/**
	 * Lookup callback for fetching sections from the extensions registry.
	 */
	#lookup: UaiTagLookupCallback = async (query: string): Promise<UaiTagItem[]> => {
		const allExtensions = umbExtensionsRegistry.getByType("section");
		const lowerQuery = query.toLowerCase();
		const results: UaiTagItem[] = [];

		for (const ext of allExtensions) {
			// Ensure it has the meta property with pathname
			if (ext.type === "section" && "meta" in ext) {
				const section = ext as any;
				const pathname = this.localize.string(section.meta?.pathname);

                // Match against alias or pathname
                const matchesQuery = pathname.toLowerCase().includes(lowerQuery);

                // Don't include already selected items
                const notSelected = !this.#items.includes(pathname);

                if (matchesQuery && notSelected) {
                    results.push({
                        id: pathname,
                        text: pathname,
                    });
                }
			}
		}

		return results;
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

export default UaiSectionTagsInputElement;

declare global {
	interface HTMLElementTagNameMap {
		"uai-section-tags-input": UaiSectionTagsInputElement;
	}
}

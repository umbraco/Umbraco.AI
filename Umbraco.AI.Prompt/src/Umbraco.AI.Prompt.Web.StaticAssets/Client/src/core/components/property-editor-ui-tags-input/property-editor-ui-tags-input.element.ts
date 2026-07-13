import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestPropertyEditorUi } from "@umbraco-cms/backoffice/property-editor";
import "@umbraco-ai/core";

/**
 * Tag item for lookup results.
 */
interface TagItem {
    id: string;
    text: string;
}

/**
 * Callback type for tag lookup.
 */
type TagLookupCallback = (query: string) => Promise<TagItem[]>;

/**
 * Gets all registered property editor UIs that back a value-holding property editor
 * (i.e. have a property editor schema alias — excludes settings-only UIs).
 */
function getAvailablePropertyEditorUis(): ManifestPropertyEditorUi[] {
    return umbExtensionsRegistry
        .getByType<"propertyEditorUi", ManifestPropertyEditorUi>("propertyEditorUi")
        .filter((manifest) => !!manifest.meta.propertyEditorSchemaAlias);
}

/**
 * A tags input component for selecting property editor UI aliases.
 * Wraps uai-tags-input with a lookup restricted to registered property editor UIs.
 *
 * @fires change - Fires when tags are added or removed
 */
@customElement("uai-property-editor-ui-tags-input")
export class UaiPropertyEditorUiTagsInputElement extends UmbLitElement {
    /**
     * The selected property editor UI aliases.
     */
    @property({ type: Array })
    public set items(value: string[]) {
        // Filter to only allow currently-registered property editor UIs
        const validAliases = new Set(getAvailablePropertyEditorUis().map((manifest) => manifest.alias));
        this.#items = (value ?? []).filter((item) => validAliases.has(item));
    }
    public get items(): string[] {
        return this.#items;
    }
    #items: string[] = [];

    /**
     * Placeholder text for the input.
     */
    @property({ type: String })
    placeholder = "Select property editor";

    /**
     * Whether the input is read-only.
     */
    @property({ type: Boolean })
    readonly = false;

    /**
     * Lookup callback for fetching registered property editor UI aliases, matched by alias or label.
     */
    #lookup: TagLookupCallback = async (query: string): Promise<TagItem[]> => {
        const lowerQuery = query.toLowerCase();

        return getAvailablePropertyEditorUis()
            .filter(
                (manifest) =>
                    manifest.alias.toLowerCase().includes(lowerQuery) ||
                    manifest.meta.label.toLowerCase().includes(lowerQuery),
            )
            .filter((manifest) => !this.#items.includes(manifest.alias)) // Exclude already selected
            .map((manifest) => ({ id: manifest.alias, text: manifest.meta.label }));
    };

    #onChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLElement & { items: string[] };
        const editors = getAvailablePropertyEditorUis();
        const aliasByLabel = new Map(editors.map((manifest) => [manifest.meta.label, manifest.alias]));
        const validAliases = new Set(editors.map((manifest) => manifest.alias));

        // Map display labels back to full aliases and validate
        const newItems = target.items
            .map((item) => (validAliases.has(item) ? item : aliasByLabel.get(item) ?? item))
            .filter((item) => validAliases.has(item));

        this.#items = newItems;
        this.dispatchEvent(new UmbChangeEvent());
    }

    render() {
        // Display friendly labels in the tags
        const labelByAlias = new Map(getAvailablePropertyEditorUis().map((manifest) => [manifest.alias, manifest.meta.label]));
        const displayItems = this.#items.map((item) => labelByAlias.get(item) ?? item);

        return html`
            <uai-tags-input
                .items=${displayItems}
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

export default UaiPropertyEditorUiTagsInputElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-property-editor-ui-tags-input": UaiPropertyEditorUiTagsInputElement;
    }
}

import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import "@umbraco-ai/core";
import { TEXT_BASED_PROPERTY_EDITOR_UIS } from "../../../prompt/property-actions/constants.js";

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
 * A tags input component for selecting text-based property editor UI aliases.
 * Wraps uai-tags-input with a restricted lookup that only allows text-based editors.
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
        // Filter to only allow valid text-based property editor UIs
        this.#items = (value ?? []).filter((item) =>
            TEXT_BASED_PROPERTY_EDITOR_UIS.includes(item as typeof TEXT_BASED_PROPERTY_EDITOR_UIS[number])
        );
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
     * Lookup callback for fetching text-based property editor UI aliases.
     * Only returns aliases from the TEXT_BASED_PROPERTY_EDITOR_UIS list.
     */
    #lookup: TagLookupCallback = async (query: string): Promise<TagItem[]> => {
        const lowerQuery = query.toLowerCase();

        return TEXT_BASED_PROPERTY_EDITOR_UIS
            .filter((alias) => {
                // Match against full alias or simplified name
                const simpleName = alias.replace("Umb.PropertyEditorUi.", "");
                return (
                    alias.toLowerCase().includes(lowerQuery) ||
                    simpleName.toLowerCase().includes(lowerQuery)
                );
            })
            .filter((alias) => !this.#items.includes(alias)) // Exclude already selected
            .map((alias) => ({
                id: alias,
                text: alias.replace("Umb.PropertyEditorUi.", ""), // Show simplified name
            }));
    };

    #onChange(event: Event) {
        event.stopPropagation();
        const target = event.target as HTMLElement & { items: string[] };

        // Map simplified names back to full aliases and validate
        const newItems = target.items.map((item) => {
            // If it's already a full alias, use it
            if (TEXT_BASED_PROPERTY_EDITOR_UIS.includes(item as typeof TEXT_BASED_PROPERTY_EDITOR_UIS[number])) {
                return item;
            }
            // Try to find matching full alias from simplified name
            const fullAlias = TEXT_BASED_PROPERTY_EDITOR_UIS.find(
                (alias) => alias.replace("Umb.PropertyEditorUi.", "") === item
            );
            return fullAlias ?? item;
        }).filter((item) =>
            TEXT_BASED_PROPERTY_EDITOR_UIS.includes(item as typeof TEXT_BASED_PROPERTY_EDITOR_UIS[number])
        );

        this.#items = newItems;
        this.dispatchEvent(new UmbChangeEvent());
    }

    render() {
        // Display simplified names in the tags
        const displayItems = this.#items.map((item) =>
            item.replace("Umb.PropertyEditorUi.", "")
        );

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

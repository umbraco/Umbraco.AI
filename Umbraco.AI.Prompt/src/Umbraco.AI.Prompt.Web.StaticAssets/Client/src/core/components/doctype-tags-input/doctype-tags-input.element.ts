import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { tryExecute } from "@umbraco-cms/backoffice/resources";
import "@umbraco-ai/core";
import { UtilsService } from "../../../api/index.js";

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
 * A tags input component for selecting document type aliases.
 * Wraps uai-tags-input with automatic lookup from the Utils API.
 *
 * @fires change - Fires when tags are added or removed
 */
@customElement("uai-doctype-tags-input")
export class UaiDoctypeTagsInputElement extends UmbLitElement {
    /**
     * The selected document type aliases.
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
    placeholder = "Enter document type alias";

    /**
     * Whether the input is read-only.
     */
    @property({ type: Boolean })
    readonly = false;

    /**
     * Lookup callback for fetching document type aliases.
     */
    #lookup: TagLookupCallback = async (query: string): Promise<TagItem[]> => {
        const { data } = await tryExecute(
            this,
            UtilsService.getDocumentTypeAliases({ query: { query } })
        );

        if (data) {
            return data.map((alias) => ({
                id: alias,
                text: alias,
            }));
        }
        return [];
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

export default UaiDoctypeTagsInputElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-doctype-tags-input": UaiDoctypeTagsInputElement;
    }
}

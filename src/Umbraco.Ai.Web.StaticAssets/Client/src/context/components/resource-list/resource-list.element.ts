import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { customElement, html, ifDefined, nothing, repeat } from "@umbraco-cms/backoffice/external/lit";


@customElement("uai-resource-list")
export class UaiResourceListElement extends UmbLitElement {

    override render() {
        return html`<div class="container">${this.#renderItems()} ${this.#renderAddButton()}</div>`;
    }

    #renderItems() {
        if (!this._items?.length) return nothing;
        return html`
			${repeat(
                this._items,
                (item) => item.unique,
                (item) => this.#renderItem(item),
            )}
		`;
    }

    #renderItem(item: any) {
        return html`
			<uui-card-media
				name=${ifDefined(item.name === null ? undefined : item.name)}
				data-mark="${item.entityType}:${item.unique}">
				<umb-imaging-thumbnail
					unique=${item.unique}
					alt=${item.name}
					icon=${item.mediaType.icon}></umb-imaging-thumbnail>
				<uui-action-bar slot="actions"> ${this.#renderRemoveAction(item)}</uui-action-bar>
			</uui-card-media>
		`;
    }

    #renderAddButton() {
        if (!this._items?.length) return nothing;
        return html`
            <uui-button
                id="btn-add"
                look="placeholder"
                @click=${this.#openPicker}
                label=${this.localize.term('general_choose')}
                <uui-icon name="icon-add"></uui-icon>
                ${this.localize.term('general_choose')}
            </uui-button>
        `;
    }
    
}

export default UaiResourceListElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-resource-list": UaiResourceListElement;
    }
}

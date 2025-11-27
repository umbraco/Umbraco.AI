import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiConnectionItemModel } from "../../../types.js";
import { UAI_CONNECTION_ICON, UAI_CONNECTION_ENTITY_TYPE } from "../../../constants.js";

/**
 * Table view for the Connection collection.
 */
@customElement("uai-connection-table-collection-view")
export class UaiConnectionTableCollectionViewElement extends UmbLitElement {
    @state()
    private _items: UmbTableItem[] = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Provider", alias: "provider" },
        { name: "Status", alias: "status" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (ctx) => {
            if (ctx) {
                this.observe(ctx.items, (items) => this.#createTableItems(items as UaiConnectionItemModel[]));
            }
        });
    }

    #createTableItems(items: UaiConnectionItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_CONNECTION_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href="section/settings/workspace/${UAI_CONNECTION_ENTITY_TYPE}/${item.unique}"
                        >${item.name}</a
                    >`,
                },
                { columnAlias: "provider", value: item.providerId },
                {
                    columnAlias: "status",
                    value: html`<uui-tag color=${item.isActive ? "positive" : "danger"}>
                        ${item.isActive ? "Active" : "Inactive"}
                    </uui-tag>`,
                },
            ],
        }));
    }

    render() {
        return html`<umb-table .columns=${this._columns} .items=${this._items}></umb-table>`;
    }

    static styles = [UmbTextStyles];
}

export default UaiConnectionTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-table-collection-view": UaiConnectionTableCollectionViewElement;
    }
}

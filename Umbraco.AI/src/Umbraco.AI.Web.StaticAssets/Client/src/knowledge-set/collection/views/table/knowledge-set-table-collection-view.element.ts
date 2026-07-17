import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem, UmbTableConfig } from "@umbraco-cms/backoffice/components";
import type { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiKnowledgeSetItemModel } from "../../../types.js";
import { UAI_KNOWLEDGE_SET_ICON } from "../../../constants.js";

/**
 * Read-only table view for the Knowledge Set collection.
 *
 * Unlike the Context table view, rows are not selectable (no bulk actions exist) and names are not
 * linked to a workspace (the per-set audit workspace is added in a later phase).
 */
@customElement("uai-knowledge-set-table-collection-view")
export class UaiKnowledgeSetTableCollectionViewElement extends UmbLitElement {
    #collectionContext?: UmbDefaultCollectionContext<UaiKnowledgeSetItemModel>;

    @state()
    private _tableConfig: UmbTableConfig = {
        allowSelection: false,
    };

    @state()
    private _items: UmbTableItem[] = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Description", alias: "description" },
        { name: "Items", alias: "itemCount" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (instance) => {
            this.#collectionContext = instance;
            this.#observeCollectionItems();
        });
    }

    #observeCollectionItems() {
        if (!this.#collectionContext) return;

        this.observe(
            this.#collectionContext.items,
            (items) => this.#createTableItems(items as UaiKnowledgeSetItemModel[]),
            "umbCollectionItemsObserver",
        );
    }

    #createTableItems(items: UaiKnowledgeSetItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: item.icon ?? UAI_KNOWLEDGE_SET_ICON,
            data: [
                { columnAlias: "name", value: item.name },
                { columnAlias: "description", value: item.description ?? "-" },
                { columnAlias: "itemCount", value: item.itemCount.toString() },
            ],
        }));
    }

    render() {
        return html`<umb-table
            .config=${this._tableConfig}
            .columns=${this._columns}
            .items=${this._items}
        ></umb-table>`;
    }

    static styles = [UmbTextStyles];
}

export default UaiKnowledgeSetTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-knowledge-set-table-collection-view": UaiKnowledgeSetTableCollectionViewElement;
    }
}

import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem } from "@umbraco-cms/backoffice/components";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiPromptItemModel } from "../../../types.js";
import { UAI_PROMPT_ICON } from "../../../constants.js";
import { UAI_EDIT_PROMPT_WORKSPACE_PATH_PATTERN } from "../../../workspace/prompt/paths.js";

/**
 * Table view for the Prompt collection.
 */
@customElement("uai-prompt-table-collection-view")
export class UaiPromptTableCollectionViewElement extends UmbLitElement {
    @state()
    private _items: UmbTableItem[] = [];

    private _columns: UmbTableColumn[] = [
        { name: "Name", alias: "name" },
        { name: "Alias", alias: "alias" },
        { name: "Description", alias: "description" },
        { name: "Active", alias: "isActive" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (ctx) => {
            if (ctx) {
                this.observe(ctx.items, (items) => this.#createTableItems(items as UaiPromptItemModel[]));
            }
        });
    }

    #createTableItems(items: UaiPromptItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_PROMPT_ICON,
            data: [
                {
                    columnAlias: "name",
                    value: html`<a
                        href=${UAI_EDIT_PROMPT_WORKSPACE_PATH_PATTERN.generateAbsolute({ unique: item.unique })}
                        >${item.name}</a
                    >`,
                },
                {
                    columnAlias: "alias",
                    value: html`<uui-tag color="primary" look="secondary">${item.alias}</uui-tag>`,
                },
                {
                    columnAlias: "description",
                    value: item.description ?? "-",
                },
                {
                    columnAlias: "isActive",
                    value: html`<uui-tag color="${item.isActive ? 'positive' : 'default'}" look="outline">
                        ${item.isActive ? 'Active' : 'Inactive'}
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

export default UaiPromptTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-table-collection-view": UaiPromptTableCollectionViewElement;
    }
}

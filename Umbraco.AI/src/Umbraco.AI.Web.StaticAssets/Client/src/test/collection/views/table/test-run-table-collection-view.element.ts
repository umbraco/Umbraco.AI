import { html, customElement, state, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type {
    UmbTableColumn,
    UmbTableItem,
    UmbTableConfig,
    UmbTableSelectedEvent,
    UmbTableDeselectedEvent,
    UmbTableElement,
} from "@umbraco-cms/backoffice/components";
import type { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { formatDateTime } from "../../../../core/index.js";
import type { UaiTestRunItemModel } from "../../../types.js";
import { UAI_TEST_RUN_ICON } from "../../../constants.js";
import { UAI_TEST_RUN_DETAIL_MODAL } from "../../../modals/test-run-detail/test-run-detail-modal.token.js";

/**
 * Table view for the Test Run collection.
 */
@customElement("uai-test-run-table-collection-view")
export class UaiTestRunTableCollectionViewElement extends UmbLitElement {
    @state()
    private _tableConfig: UmbTableConfig = {
        allowSelection: true,
    };

    @state()
    private _items: UmbTableItem[] = [];

    @state()
    private _selection: Array<string> = [];

    #collectionContext?: UmbDefaultCollectionContext<UaiTestRunItemModel>;

    private _columns: UmbTableColumn[] = [
        { name: "Run ID", alias: "runId" },
        { name: "Status", alias: "status" },
        { name: "Test ID", alias: "testId" },
        { name: "Run #", alias: "runNumber" },
        { name: "Duration", alias: "duration" },
        { name: "Executed At", alias: "executedAt" },
        { name: "Batch ID", alias: "batchId" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (instance) => {
            this.#collectionContext = instance;
            this.#collectionContext?.selection.setSelectable(true);
            this.#observeCollectionItems();
        });
    }

    #observeCollectionItems() {
        if (!this.#collectionContext) return;

        this.observe(
            this.#collectionContext.items,
            (items) => this.#createTableItems(items as UaiTestRunItemModel[]),
            "umbCollectionItemsObserver",
        );

        this.observe(
            this.#collectionContext.selection.selection,
            (selection) => {
                this._selection = selection as string[];
            },
            "umbCollectionSelectionObserver",
        );
    }

    #getStatusColor(status: string): string {
        switch (status.toLowerCase()) {
            case "passed":
                return "positive";
            case "failed":
            case "error":
                return "danger";
            case "running":
                return "warning";
            default:
                return "default";
        }
    }

    #formatDuration(ms: number): string {
        if (ms < 1000) return `${ms}ms`;
        if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
        return `${(ms / 60000).toFixed(1)}m`;
    }

    #truncateGuid(guid: string): string {
        return guid.length > 8 ? guid.substring(0, 8) + "..." : guid;
    }

    async #openRunDetail(runId: string) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        modalManager.open(this, UAI_TEST_RUN_DETAIL_MODAL, {
            data: { runId },
        });
    }

    #createTableItems(items: UaiTestRunItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: UAI_TEST_RUN_ICON,
            data: [
                {
                    columnAlias: "runId",
                    value: html`<a
                        href="#"
                        class="run-link"
                        title=${item.unique}
                        @click=${(e: Event) => {
                            e.preventDefault();
                            e.stopPropagation();
                            this.#openRunDetail(item.unique);
                        }}
                        >${this.#truncateGuid(item.unique)}</a
                    >`,
                },
                {
                    columnAlias: "status",
                    value: html`<uui-tag
                        color=${this.#getStatusColor(item.status)}
                        look="primary"
                        >${item.status}</uui-tag
                    >`,
                },
                {
                    columnAlias: "testId",
                    value: html`<span title=${item.testId}>${this.#truncateGuid(item.testId)}</span>`,
                },
                {
                    columnAlias: "runNumber",
                    value: `#${item.runNumber}`,
                },
                {
                    columnAlias: "duration",
                    value: this.#formatDuration(item.durationMs),
                },
                {
                    columnAlias: "executedAt",
                    value: item.executedAt ? formatDateTime(item.executedAt) : "-",
                },
                {
                    columnAlias: "batchId",
                    value: item.batchId
                        ? html`<span title=${item.batchId}>${this.#truncateGuid(item.batchId)}</span>`
                        : "-",
                },
            ],
        }));
    }

    #handleSelect(event: UmbTableSelectedEvent) {
        event.stopPropagation();
        const table = event.target as UmbTableElement;
        this.#collectionContext?.selection.setSelection(table.selection);
    }

    #handleDeselect(event: UmbTableDeselectedEvent) {
        event.stopPropagation();
        const table = event.target as UmbTableElement;
        this.#collectionContext?.selection.setSelection(table.selection);
    }

    render() {
        return html`<umb-table
            .config=${this._tableConfig}
            .columns=${this._columns}
            .items=${this._items}
            .selection=${this._selection}
            @selected=${this.#handleSelect}
            @deselected=${this.#handleDeselect}
        ></umb-table>`;
    }

    static styles = [
        UmbTextStyles,
        css`
            .run-link {
                color: var(--uui-color-interactive);
                text-decoration: none;
                font-weight: 600;
            }
            .run-link:hover {
                text-decoration: underline;
            }
        `,
    ];
}

export default UaiTestRunTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-table-collection-view": UaiTestRunTableCollectionViewElement;
    }
}

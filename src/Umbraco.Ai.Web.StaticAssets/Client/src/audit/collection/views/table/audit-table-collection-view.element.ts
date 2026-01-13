import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UmbTableColumn, UmbTableItem, UmbTableConfig, UmbTableSelectedEvent, UmbTableDeselectedEvent, UmbTableElement } from "@umbraco-cms/backoffice/components";
import type { UmbDefaultCollectionContext } from "@umbraco-cms/backoffice/collection";
import { UMB_COLLECTION_CONTEXT } from "@umbraco-cms/backoffice/collection";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiAuditItemModel, UaiAuditStatus } from "../../../types.js";

/**
 * Table view for the Audit collection.
 */
@customElement("uai-audit-table-collection-view")
export class UaiAuditTableCollectionViewElement extends UmbLitElement {
    @state()
    private _tableConfig: UmbTableConfig = {
        allowSelection: true,
    };

    @state()
    private _items: UmbTableItem[] = [];

    @state()
    private _selection: Array<string> = [];

    #collectionContext?: UmbDefaultCollectionContext<UaiAuditItemModel>;

    private _columns: UmbTableColumn[] = [
        { name: "Timestamp", alias: "timestamp" },
        { name: "User", alias: "user" },
        { name: "Operation", alias: "operation" },
        { name: "Feature", alias: "feature" },
        { name: "Model", alias: "model" },
        { name: "Status", alias: "status" },
        { name: "Duration", alias: "duration" },
        { name: "Tokens", alias: "tokens" },
        { name: "", alias: "error" },
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
            (items) => this.#createTableItems(items as UaiAuditItemModel[]),
            "umbCollectionItemsObserver"
        );

        this.observe(
            this.#collectionContext.selection.selection,
            (selection) => {
                this._selection = selection as string[];
            },
            "umbCollectionSelectionObserver"
        );
    }

    #createTableItems(items: UaiAuditItemModel[]) {
        this._items = items.map((item) => ({
            id: item.unique,
            icon: "icon-pulse",
            data: [
                {
                    columnAlias: "timestamp",
                    value: this.#formatTimestamp(item.startTime),
                },
                {
                    columnAlias: "user",
                    value: item.userName ?? item.userId,
                },
                {
                    columnAlias: "operation",
                    value: item.operationType,
                },
                {
                    columnAlias: "feature",
                    value: item.featureType
                        ? html`<div style="font-size: 0.9em;">
                            <div style="text-transform: capitalize;">${item.featureType}</div>
                            ${item.featureId ? html`<div style="color: var(--uui-color-text-alt); font-size: 0.75em; font-family: monospace;">${item.featureId}</div>` : ""}
                          </div>`
                        : "—",
                },
                {
                    columnAlias: "model",
                    value: html`<div style="font-size: 0.9em;">
                        <div>${item.modelId}</div>
                        <div style="color: var(--uui-color-text-alt); font-size: 0.85em;">${item.providerId}</div>
                    </div>`,
                },
                {
                    columnAlias: "status",
                    value: this.#renderStatusBadge(item.status),
                },
                {
                    columnAlias: "duration",
                    value: item.durationMs ? `${(item.durationMs / 1000).toFixed(2)}s` : "—",
                },
                {
                    columnAlias: "tokens",
                    value: html`<div style="font-size: 0.9em;">
                        ${item.inputTokens ?? 0} / ${item.outputTokens ?? 0}
                    </div>`,
                },
                {
                    columnAlias: "error",
                    value: item.errorMessage
                        ? html`<uui-icon name="icon-alert" style="color: var(--uui-color-danger);"></uui-icon>`
                        : html``,
                },
            ],
        }));
    }

    #formatTimestamp(timestamp: string): string {
        const date = new Date(timestamp);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return "Just now";
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        if (diffDays < 7) return `${diffDays}d ago`;

        return date.toLocaleDateString() + " " + date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    }

    #getStatusColor(status: UaiAuditStatus): string {
        switch (status) {
            case "Succeeded":
                return "positive";
            case "Failed":
                return "danger";
            case "Running":
                return "default";
            case "Cancelled":
                return "warning";
            case "PartialSuccess":
                return "warning";
            default:
                return "default";
        }
    }

    #renderStatusBadge(status: UaiAuditStatus) {
        return html`<uui-tag color=${this.#getStatusColor(status)} look="primary">
            ${status}
        </uui-tag>`;
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
            @deselected=${this.#handleDeselect}></umb-table>`;
    }

    static styles = [UmbTextStyles];
}

export default UaiAuditTableCollectionViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-audit-table-collection-view": UaiAuditTableCollectionViewElement;
    }
}

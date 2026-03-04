import { html, customElement, state, css, nothing } from "@umbraco-cms/backoffice/external/lit";
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
import type { UaiTestRunItemModel } from "../../../types.js";
import { UAI_TEST_RUN_ICON } from "../../../constants.js";
import { UAI_TEST_RUN_DETAIL_MODAL } from "../../../modals/test-run-detail/test-run-detail-modal.token.js";
import { UAI_TEST_WORKSPACE_CONTEXT } from "../../../workspace/test/test-workspace.context-token.js";

interface RunMetrics {
    totalRuns: number;
    passedRuns: number;
    failedRuns: number;
    passAtK: number;
    passToTheK: number;
}

interface ExecutionGroupInfo {
    runCount: number;
    variationCount: number;
    firstDate: string;
}

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

    @state()
    private _metrics?: RunMetrics;

    @state()
    private _baselineRunId?: string | null;

    #runItems = new Map<string, UaiTestRunItemModel>();
    #collectionContext?: UmbDefaultCollectionContext<UaiTestRunItemModel>;

    private _columns: UmbTableColumn[] = [
        { name: "Execution", alias: "execution" },
        { name: "Test", alias: "testId" },
        { name: "Variation / Run", alias: "variationRun" },
        { name: "Status", alias: "status" },
        { name: "Duration", alias: "duration" },
        { name: "", alias: "entityActions", align: "right" },
    ];

    constructor() {
        super();
        this.consumeContext(UMB_COLLECTION_CONTEXT, (instance) => {
            this.#collectionContext = instance;
            this.#collectionContext?.selection.setSelectable(true);
            this.#observeCollectionItems();
        });
        this.consumeContext(UAI_TEST_WORKSPACE_CONTEXT, (context) => {
            if (!context) return;
            this.observe(
                context.model,
                (model) => {
                    this._baselineRunId = model?.baselineRunId;
                },
                "umbBaselineRunIdObserver",
            );
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

    async #openRunDetail(runId: string) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        // Use workspace context baseline if available, otherwise fall back to the run's own baselineRunId
        const baselineRunId = this._baselineRunId ?? this.#runItems.get(runId)?.baselineRunId;

        modalManager.open(this, UAI_TEST_RUN_DETAIL_MODAL, {
            data: { runId, baselineRunId: baselineRunId ?? undefined },
        });
    }

    #computeMetrics(items: UaiTestRunItemModel[]) {
        const completed = items.filter((i) => {
            const s = i.status.toLowerCase();
            return s === "passed" || s === "failed" || s === "error";
        });
        if (completed.length === 0) {
            this._metrics = undefined;
            return;
        }
        const passedRuns = completed.filter((i) => i.status.toLowerCase() === "passed").length;
        const totalRuns = completed.length;
        this._metrics = {
            totalRuns,
            passedRuns,
            failedRuns: totalRuns - passedRuns,
            passAtK: totalRuns > 0 ? passedRuns / totalRuns : 0,
            passToTheK: passedRuns === totalRuns ? 1.0 : 0.0,
        };
    }

    #buildExecutionGroups(items: UaiTestRunItemModel[]): Map<string, ExecutionGroupInfo> {
        const groups = new Map<string, ExecutionGroupInfo>();
        for (const item of items) {
            const key = item.executionId ?? item.unique;
            const existing = groups.get(key);
            if (existing) {
                existing.runCount++;
                if (item.variationName) {
                    existing.variationCount++;
                }
                if (item.executedAt && item.executedAt < existing.firstDate) {
                    existing.firstDate = item.executedAt;
                }
            } else {
                groups.set(key, {
                    runCount: 1,
                    variationCount: item.variationName ? 1 : 0,
                    firstDate: item.executedAt ?? "",
                });
            }
        }
        return groups;
    }

    #sortByExecution(items: UaiTestRunItemModel[]): UaiTestRunItemModel[] {
        const groups = new Map<string, UaiTestRunItemModel[]>();
        for (const item of items) {
            const key = item.executionId ?? item.unique;
            const group = groups.get(key);
            if (group) {
                group.push(item);
            } else {
                groups.set(key, [item]);
            }
        }

        // Sort groups by newest execution first (max executedAt in group)
        const sortedGroups = [...groups.entries()].sort(([, a], [, b]) => {
            const maxA = a.reduce((max, i) => (i.executedAt && i.executedAt > max ? i.executedAt : max), "");
            const maxB = b.reduce((max, i) => (i.executedAt && i.executedAt > max ? i.executedAt : max), "");
            return maxB.localeCompare(maxA);
        });

        // Sort within each group: Default (no variationName) first, then by variationName, then by runNumber
        const sorted: UaiTestRunItemModel[] = [];
        for (const [, group] of sortedGroups) {
            group.sort((a, b) => {
                const aVar = a.variationName ?? "";
                const bVar = b.variationName ?? "";
                if (aVar !== bVar) {
                    if (!a.variationName) return -1;
                    if (!b.variationName) return 1;
                    return aVar.localeCompare(bVar);
                }
                return a.runNumber - b.runNumber;
            });
            sorted.push(...group);
        }

        return sorted;
    }

    #formatShortDate(dateStr: string): string {
        const date = new Date(dateStr);
        return date.toLocaleDateString(undefined, { month: "short", day: "numeric" }) +
            ", " +
            date.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
    }

    #createTableItems(items: UaiTestRunItemModel[]) {
        this.#computeMetrics(items);
        this.#runItems = new Map(items.map((item) => [item.unique, item]));

        const sorted = this.#sortByExecution(items);
        const executionGroups = this.#buildExecutionGroups(sorted);
        let prevExecutionId: string | undefined;

        this._items = sorted.map((item) => {
            const execKey = item.executionId ?? item.unique;
            const isFirstInGroup = execKey !== prevExecutionId;
            prevExecutionId = execKey;

            const groupInfo = executionGroups.get(execKey);
            let executionCell;
            if (isFirstInGroup && groupInfo && item.executionId) {
                const varsLine = groupInfo.variationCount > 0
                    ? html`<span class="exec-info">${groupInfo.variationCount} variations</span>`
                    : nothing;
                executionCell = html`<div class="exec-badge">
                    <span class="exec-date">${this.#formatShortDate(groupInfo.firstDate)}</span>
                    <span class="exec-info">${groupInfo.runCount} runs</span>
                    ${varsLine}
                </div>`;
            } else {
                executionCell = "";
            }

            return {
                id: item.unique,
                icon: item.isBaseline ? "icon-flag color-green" : UAI_TEST_RUN_ICON,
                data: [
                    {
                        columnAlias: "execution",
                        value: executionCell,
                    },
                    {
                        columnAlias: "testId",
                        value: html`<div class="test-cell">
                            <div class="test-name">${item.testName ?? item.testId}</div>
                            <div class="test-id" title=${item.testId}>${item.testId}</div>
                        </div>`,
                    },
                    {
                        columnAlias: "variationRun",
                        value: html`<a
                            href="#"
                            class="run-link"
                            title=${item.unique}
                            @click=${(e: Event) => {
                                e.preventDefault();
                                e.stopPropagation();
                                this.#openRunDetail(item.unique);
                            }}
                            ><uui-tag look="secondary"
                                >${item.variationName ?? "Default"}</uui-tag
                            >
                            <span class="run-number">#${item.runNumber}</span></a
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
                        columnAlias: "duration",
                        value: this.#formatDuration(item.durationMs),
                    },
                    {
                        columnAlias: "entityActions",
                        value: html`<umb-entity-actions-table-column-view
                            .value=${{ entityType: item.entityType, unique: item.unique }}
                        ></umb-entity-actions-table-column-view>`,
                    },
                ],
            };
        });
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

    #renderMetrics() {
        if (!this._metrics || this._metrics.totalRuns === 0) return nothing;

        const m = this._metrics;
        const passPercent = m.passAtK * 100;
        const barClass = m.failedRuns === 0 ? "success" : passPercent >= 50 ? "partial" : "failure";

        return html`
            <div class="metrics-panel">
                <div class="metrics-grid">
                    <div class="metric">
                        <span class="metric-label">Total Runs</span>
                        <span class="metric-value">${m.totalRuns}</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">Passed</span>
                        <span class="metric-value ${barClass}">${m.passedRuns}/${m.totalRuns}</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">pass@k</span>
                        <span class="metric-value">${passPercent.toFixed(1)}%</span>
                        <span class="metric-desc">&ge;1 success</span>
                    </div>
                    <div class="metric">
                        <span class="metric-label">pass^k</span>
                        <span class="metric-value">${(m.passToTheK * 100).toFixed(1)}%</span>
                        <span class="metric-desc">all succeed</span>
                    </div>
                </div>
                <div class="metric-bar">
                    <div class="metric-bar-fill ${barClass}" style="width: ${passPercent}%"></div>
                </div>
            </div>
        `;
    }

    render() {
        return html`
            ${this.#renderMetrics()}
            <umb-table
                .config=${this._tableConfig}
                .columns=${this._columns}
                .items=${this._items}
                .selection=${this._selection}
                @selected=${this.#handleSelect}
                @deselected=${this.#handleDeselect}
            ></umb-table>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            .run-link {
                display: inline-flex;
                align-items: center;
                gap: 6px;
                color: var(--uui-color-interactive);
                text-decoration: none;
            }
            .run-link:hover {
                text-decoration: underline;
            }

            .run-number {
                font-weight: 600;
            }

            .test-cell {
                font-size: 0.9em;
                line-height: 1.5;
                padding: 5px 0;
            }

            .test-id {
                color: var(--uui-palette-dusty-grey-dark);
                font-size: 11px;
                font-family: monospace;
            }

            .metrics-panel {
                display: flex;
                flex-direction: column;
                gap: 10px;
                padding: 16px 20px;
                background: var(--uui-color-surface);
                border: 1px solid var(--uui-color-border);
                border-radius: 6px;
                margin-bottom: 16px;
            }

            .metrics-grid {
                display: grid;
                grid-template-columns: repeat(4, 1fr);
                gap: 16px;
            }

            .metric {
                display: flex;
                flex-direction: column;
                align-items: center;
                gap: 2px;
                min-height: 52px;
            }

            .metric-label {
                font-size: 12px;
                color: var(--uui-color-text-alt);
                font-weight: 500;
                text-transform: uppercase;
                letter-spacing: 0.5px;
            }

            .metric-value {
                font-size: 32px;
                font-weight: 600;
                color: var(--uui-color-text);
                line-height: 1.2;
            }

            .metric-value.success {
                color: var(--uui-color-positive);
            }

            .metric-value.partial {
                color: var(--uui-color-warning);
            }

            .metric-value.failure {
                color: var(--uui-color-danger);
            }

            .metric-desc {
                font-size: 10px;
                color: var(--uui-color-text-alt);
                line-height: 1;
            }

            .metric-bar {
                width: 100%;
                height: 6px;
                background: var(--uui-color-surface-alt);
                border-radius: 3px;
                overflow: hidden;
                margin-top: var(--uui-size-space-3);
            }

            .metric-bar-fill {
                height: 100%;
                transition: width 0.3s ease;
            }

            .metric-bar-fill.success {
                background: var(--uui-color-positive);
            }

            .metric-bar-fill.partial {
                background: var(--uui-color-warning);
            }

            .metric-bar-fill.failure {
                background: var(--uui-color-danger);
            }

            .exec-badge {
                display: flex;
                flex-direction: column;
                gap: 2px;
                padding: 4px 8px;
                background: var(--uui-color-surface-alt);
                border-radius: 4px;
                border-left: 3px solid var(--uui-color-interactive);
                line-height: 1.3;
            }

            .exec-date {
                font-size: 12px;
                font-weight: 600;
                color: var(--uui-color-text);
            }

            .exec-info {
                font-size: 11px;
                color: var(--uui-color-text-alt);
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

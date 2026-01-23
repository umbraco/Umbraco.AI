import {
    css,
    customElement,
    html,
    nothing,
    repeat,
    state,
    when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UMB_WORKSPACE_CONTEXT } from "@umbraco-cms/backoffice/workspace";
import { isVersionableEntityWorkspaceContext } from "../../context/index.js";
import type { UaiVersionableEntityWorkspaceContext } from "../../context/index.js";
import type { UaiVersionHistoryItem } from "../../types.js";
import { UAI_ROLLBACK_MODAL } from "../../modals/rollback-modal/rollback-modal.token.js";

const PAGE_SIZE = 10;

/**
 * A reusable version history table component that displays version history
 * for versionable entities. It consumes the workspace context and checks
 * if it supports versioning via the UaiVersionableEntityWorkspaceContext interface.
 *
 * @element uai-version-history-table
 */
@customElement("uai-version-history-table")
export class UaiVersionHistoryTableElement extends UmbLitElement {
    #workspaceContext?: UaiVersionableEntityWorkspaceContext;
    #modalManager?: typeof UMB_MODAL_MANAGER_CONTEXT.TYPE;

    @state()
    private _currentVersion?: number;

    @state()
    private _versions: UaiVersionHistoryItem[] = [];

    @state()
    private _totalVersions = 0;

    @state()
    private _loading = true;

    @state()
    private _currentPage = 1;

    @state()
    private _hasVersionSupport = false;

    constructor() {
        super();

        this.consumeContext(UMB_WORKSPACE_CONTEXT, (context) => {
            if (isVersionableEntityWorkspaceContext(context)) {
                this.#workspaceContext = context;
                this._hasVersionSupport = true;
                this.observe(context.version, (v) => {
                    this._currentVersion = v;
                    if (v !== undefined) {
                        this.#loadVersionHistory();
                    }
                });
            }
        });

        this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (context) => {
            this.#modalManager = context;
        });
    }

    async #loadVersionHistory() {
        if (!this.#workspaceContext) return;

        this._loading = true;
        try {
            const skip = (this._currentPage - 1) * PAGE_SIZE;
            const response = await this.#workspaceContext.getVersionHistory(skip, PAGE_SIZE);
            if (response) {
                this._versions = response.versions;
                this._totalVersions = response.totalVersions;
                this._currentVersion = response.currentVersion;
            }
        } finally {
            this._loading = false;
        }
    }

    get #totalPages(): number {
        return Math.ceil(this._totalVersions / PAGE_SIZE);
    }

    #formatDate(dateString: string): string {
        const date = new Date(dateString);
        return date.toLocaleDateString(undefined, {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
        });
    }

    async #onCompareClick(version: number) {
        if (!this.#workspaceContext || !this.#modalManager || this._currentVersion === undefined) return;

        // Get comparison data
        const comparison = await this.#workspaceContext.compareVersions(version, this._currentVersion);
        if (!comparison) return;

        // Open rollback modal
        const modalContext = this.#modalManager.open(this, UAI_ROLLBACK_MODAL, {
            data: {
                fromVersion: version,
                toVersion: this._currentVersion,
                changes: comparison.changes,
            },
        });

        const result = await modalContext.onSubmit().catch(() => undefined);
        if (result?.rollback) {
            await this.#workspaceContext.rollbackToVersion(version);
            await this.#loadVersionHistory();
        }
    }

    #onPreviousPage() {
        if (this._currentPage > 1) {
            this._currentPage--;
            this.#loadVersionHistory();
        }
    }

    #onNextPage() {
        if (this._currentPage < this.#totalPages) {
            this._currentPage++;
            this.#loadVersionHistory();
        }
    }

    override render() {
        // Don't render if the workspace doesn't support versioning
        if (!this._hasVersionSupport) {
            return nothing;
        }

        return html`
            <uui-box headline=${this.localize.term("uaiVersionHistory_history")}>
                ${when(
                    this._loading,
                    () => html`
                        <div class="loading">
                            <uui-loader></uui-loader>
                        </div>
                    `,
                    () => this.#renderContent()
                )}
            </uui-box>
        `;
    }

    #renderContent() {
        if (this._versions.length === 0) {
            return html`
                <p class="no-versions">
                    ${this.localize.term("uaiVersionHistory_noVersionsYet")}
                </p>
            `;
        }

        return html`
            <uui-table>
                <uui-table-head>
                    <uui-table-head-cell>
                        ${this.localize.term("uaiVersionHistory_version")}
                    </uui-table-head-cell>
                    <uui-table-head-cell>
                        ${this.localize.term("uaiVersionHistory_date")}
                    </uui-table-head-cell>
                    <uui-table-head-cell>
                        ${this.localize.term("uaiVersionHistory_user")}
                    </uui-table-head-cell>
                    <uui-table-head-cell></uui-table-head-cell>
                </uui-table-head>
                ${repeat(
                    this._versions,
                    (v) => v.id,
                    (v) => this.#renderRow(v)
                )}
            </uui-table>
            ${this.#totalPages > 1 ? this.#renderPagination() : nothing}
        `;
    }

    #renderRow(version: UaiVersionHistoryItem) {
        const isCurrent = version.version === this._currentVersion;
        return html`
            <uui-table-row>
                <uui-table-cell>
                    <span class="version-label ${isCurrent ? "current" : ""}">
                        v${version.version}
                        ${isCurrent ? html`<span class="current-badge">(${this.localize.term("uaiVersionHistory_current")})</span>` : nothing}
                    </span>
                </uui-table-cell>
                <uui-table-cell>
                    ${this.#formatDate(version.dateCreated)}
                </uui-table-cell>
                <uui-table-cell>
                    ${version.createdByUserName ?? version.createdByUserId ?? "-"}
                </uui-table-cell>
                <uui-table-cell class="actions-cell">
                    ${!isCurrent
                        ? html`
                            <uui-button
                                look="secondary"
                                label=${this.localize.term("uaiVersionHistory_compare")}
                                @click=${() => this.#onCompareClick(version.version)}>
                                ${this.localize.term("uaiVersionHistory_compare")}
                            </uui-button>
                        `
                        : nothing}
                </uui-table-cell>
            </uui-table-row>
        `;
    }

    #renderPagination() {
        return html`
            <div class="pagination">
                <uui-button
                    look="secondary"
                    compact
                    ?disabled=${this._currentPage <= 1}
                    @click=${this.#onPreviousPage}>
                    <uui-icon name="icon-arrow-left"></uui-icon>
                </uui-button>
                <span class="page-info">
                    ${this.localize.term("uaiVersionHistory_pageInfo", [
                        this._currentPage,
                        this.#totalPages,
                    ])}
                </span>
                <uui-button
                    look="secondary"
                    compact
                    ?disabled=${this._currentPage >= this.#totalPages}
                    @click=${this.#onNextPage}>
                    <uui-icon name="icon-arrow-right"></uui-icon>
                </uui-button>
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .loading {
                display: flex;
                justify-content: center;
                padding: var(--uui-size-space-5);
            }

            .no-versions {
                text-align: center;
                color: var(--uui-color-text-alt);
                padding: var(--uui-size-space-5);
                margin: 0;
            }

            uui-table {
                width: 100%;
                margin-top: calc(var(--uui-size-space-3) * -1);
            }

            .version-label {
                font-weight: 500;
            }

            .version-label.current {
                color: var(--uui-color-positive);
            }

            .current-badge {
                font-weight: normal;
                font-size: 0.85em;
                margin-left: var(--uui-size-space-2);
            }

            .actions-cell {
                text-align: right;
            }

            .pagination {
                display: flex;
                justify-content: center;
                align-items: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-4) 0;
                border-top: 1px solid var(--uui-color-border);
                margin-top: var(--uui-size-space-4);
            }

            .page-info {
                color: var(--uui-color-text-alt);
                font-size: 0.9em;
            }
        `,
    ];
}

export default UaiVersionHistoryTableElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-version-history-table": UaiVersionHistoryTableElement;
    }
}

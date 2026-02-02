import {
    css,
    customElement,
    html,
    nothing,
    property,
    repeat,
    state,
    when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import type { UaiVersionHistoryItem } from "../../types.js";
import { UAI_ROLLBACK_MODAL } from "../../modals/rollback-modal/rollback-modal.token.js";
import { UaiUnifiedVersionHistoryRepository } from "../../repository/unified-version-history.repository.js";
import { UmbUserItemModel, UmbUserItemRepository } from "@umbraco-cms/backoffice/user";
import { formatDateTime } from "../../../utils";
import { UAI_EMPTY_GUID } from "../../../index.js";

const PAGE_SIZE = 10;

/**
 * A reusable version history component that displays version history
 * for versionable entities. Pass the entity type and ID as attributes.
 *
 * @element uai-version-history
 * @fires rollback - Fired after a successful rollback so parent can reload entity data
 */
@customElement("uai-version-history")
export class UaiVersionHistoryElement extends UmbLitElement {
    
    #versionRepository = new UaiUnifiedVersionHistoryRepository(this);
    #userItemRepository = new UmbUserItemRepository(this);
    
    #modalManager?: typeof UMB_MODAL_MANAGER_CONTEXT.TYPE;

    #userMap = new Map<string, UmbUserItemModel>();

    /**
     * The entity type for version history operations.
     */
    @property({ type: String, attribute: "entity-type" })
    entityType?: string;

    /**
     * The unique identifier of the entity.
     */
    @property({ type: String, attribute: "entity-id" })
    entityId?: string;

    /**
     * The current version of the entity.
     * When this changes, the version history is refreshed.
     */
    @property({ attribute: false })
    currentVersion?: number;

    @state()
    private _currentVersion?: number;

    @state()
    private _versions: UaiVersionHistoryItem[] = [];

    @state()
    private _totalVersions = 0;

    @state()
    private _loading = false;

    @state()
    private _currentPage = 1;

    constructor() {
        super();

        this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (context) => {
            this.#modalManager = context;
        });
    }

    override connectedCallback() {
        super.connectedCallback();
        if (this.entityType && this.entityId) {
            this.#loadVersionHistory();
        }
    }

    override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);
        if (changedProperties.has("entityType") || changedProperties.has("entityId")) {
            if (this.entityType && this.entityId) {
                this._currentPage = 1;
                this.#loadVersionHistory();
            }
        }
        // Refresh when currentVersion changes (but not on initial undefined->value)
        if (changedProperties.has("currentVersion") && changedProperties.get("currentVersion") !== undefined) {
            this.#loadVersionHistory();
        }
    }

    /**
     * Refreshes the version history data.
     * Call this after entity data has been modified.
     */
    public refresh(): void {
        this.#loadVersionHistory();
    }

    async #loadVersionHistory() {
        if (!this.entityType || !this.entityId || this.entityId === UAI_EMPTY_GUID) return;

        this._loading = true;
        try {
            const skip = (this._currentPage - 1) * PAGE_SIZE;
            const response = await this.#versionRepository.getVersionHistory(
                this.entityType,
                this.entityId,
                skip,
                PAGE_SIZE
            );
            if (response) {
                this._versions = response.versions;
                this._totalVersions = response.totalVersions;
                this._currentVersion = response.currentVersion;
                this.#requestAndCacheUserItems();
            }
        } finally {
            this._loading = false;
        }
    }

    async #requestAndCacheUserItems() 
    {
        const allUsers = this._versions?.map((item) => item.createdByUserId?.toString()).filter(Boolean) as string[];
        const uniqueUsers = [...new Set(allUsers)];
        const uncachedUsers = uniqueUsers.filter((unique) => !this.#userMap.has(unique));

        // If there are no uncached user items, we don't need to make a request
        if (uncachedUsers.length === 0) return;

        const { data: items } = await this.#userItemRepository.requestItems(uncachedUsers);

        if (items) {
            items.forEach((item) => {
                // cache the user item
                this.#userMap.set(item.unique, item);
                this.requestUpdate('_versions');
            });
        }
    }

    get #totalPages(): number {
        return Math.ceil(this._totalVersions / PAGE_SIZE);
    }

    async #onCompareClick(version: number) {
        if (!this.entityType || !this.entityId || !this.#modalManager || this._currentVersion === undefined) return;

        // Get comparison data
        const comparison = await this.#versionRepository.compareVersions(
            this.entityType,
            this.entityId,
            version,
            this._currentVersion
        );
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
            const success = await this.#versionRepository.rollback(
                this.entityType,
                this.entityId,
                version
            );
            if (success) {
                // Dispatch event so parent can reload entity data
                this.dispatchEvent(new CustomEvent("rollback", { bubbles: true, composed: true }));
                await this.#loadVersionHistory();
            }
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
        // Don't render if entity type or ID is not set
        if (!this.entityType || !this.entityId) {
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
            <div class="versions-list">
                ${repeat(
                    this._versions,
                    (v) => v.id,
                    (v) => this.#renderItem(v)
                )}
            </div>
            ${this.#totalPages > 1 ? this.#renderPagination() : nothing}
        `;
    }

    #renderItem(version: UaiVersionHistoryItem) {
        const isCurrent = version.version === this._currentVersion;
        const user = this.#userMap.get(version.createdByUserId?.toString() || "");
        return html`
            <div class="version-item">
                <div class="user-info">
                    <umb-user-avatar
                            .name=${user?.name}
                            .kind=${user?.kind}
                            .imgUrls=${user?.avatarUrls ?? []}>
                    </umb-user-avatar>
                    <div>
                        <span class="name">${user?.name}</span>
                        <span class="detail">${formatDateTime(version.dateCreated)}</span>
                    </div>
                </div>
                <div>
                    ${isCurrent ? html`
                        <div>
                            <uui-tag look="primary">
                                Current
                            </uui-tag>
                        </div>
                    `: html`
                        <uui-tag look="secondary">
                                v${version.version}
                        </uui-tag>
                    `}
                </div>
                ${!isCurrent ? html`
                    <div class="actions">
                        <uui-button
                                look="secondary"
                                label=${this.localize.term("uaiVersionHistory_compare")}
                                @click=${() => this.#onCompareClick(version.version)}>
                            ${this.localize.term("uaiVersionHistory_compare")}
                        </uui-button>
                    </div>
                `: nothing }
            </umb-history-item>
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

            .versions-list {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }

            .version-item {
                display: flex;
                gap: var(--uui-size-space-5);
                align-items: center;
            }
            
            .actions {
                flex: 1;
                display: flex;
                justify-content: flex-end;
            }

            .user-info {
                position: relative;
                display: flex;
                align-items: flex-end;
                gap: var(--uui-size-space-4);
            }

            .user-info div {
                display: flex;
                flex-direction: column;
                min-width: var(--uui-size-60);
            }

            .detail {
                font-size: var(--uui-size-4);
                color: var(--uui-color-text-alt);
                line-height: 1;
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

export default UaiVersionHistoryElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-version-history": UaiVersionHistoryElement;
    }
}

import { css, customElement, html, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UaiAuditLogDetailsModalData, UaiAuditLogDetailsModalValue } from "./audit-log-details-modal.token.js";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiAuditLogDetailRepository } from "../../../repository/detail/audit-log-detail.repository.js";
import { UaiAuditLogDetailModel, type UaiAuditLogStatus } from "../../../types.js";

@customElement("uai-audit-log-details-modal")
export class UaiAuditLogDetailsModalElement extends UmbModalBaseElement<
    UaiAuditLogDetailsModalData,
    UaiAuditLogDetailsModalValue
> {
    #auditLogDetailsRepository = new UaiAuditLogDetailRepository(this);

    @state()
    private _loading = true;

    @state()
    private _auditLog?: UaiAuditLogDetailModel;

    override async firstUpdated() {
        await this.#loadAuditLog();
    }

    async #loadAuditLog() {
        this._loading = true;
        const { data } = await this.#auditLogDetailsRepository.requestByUnique(this.value!.unique!);
        this._auditLog = data;
        this._loading = false;
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

    #renderStatusBadge(status: UaiAuditLogStatus | "Unknown") {
        return html`<uui-tag color=${this.#getStatusColor(status)} look="primary"> ${status} </uui-tag>`;
    }

    #getStatusColor(status: UaiAuditLogStatus | "Unknown"): string {
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

    #onNextClick(e: Event) {
        e.preventDefault();
        e.stopPropagation();
        const idx = this.data!.uniques!.indexOf(this.value!.unique!);
        if (idx < this.data!.uniques!.length - 1) {
            this.value = { unique: this.data!.uniques![idx + 1] };
            this.#loadAuditLog();
        }
    }

    #onPrevClick(e: Event) {
        e.preventDefault();
        e.stopPropagation();
        const idx = this.data!.uniques!.indexOf(this.value!.unique!);
        if (idx > 0) {
            this.value = { unique: this.data!.uniques![idx - 1] };
            this.#loadAuditLog();
        }
    }

    override render() {
        return html`
            <umb-body-layout>
                <div slot="header" class="header-layout">
                    <div>
                        <!-- Prev Button -->
                        <uui-button
                            look="secondary"
                            compact
                            @click=${this.#onPrevClick}
                            ?disabled=${this.data!.uniques!.indexOf(this.value!.unique!) === 0}
                        >
                            <uui-icon name="icon-arrow-left"></uui-icon>
                        </uui-button>
                    </div>
                    <div>
                        <h3>
                            Audit Log Details
                            <small
                                >[${(this.data?.uniques?.indexOf(this.value?.unique ?? this.data?.uniques[0]) ?? 0) +
                                1}/${this.data?.uniques?.length ?? 0}]</small
                            >
                        </h3>
                        <small><strong>ID:</strong> ${this._auditLog?.unique}</small>
                    </div>
                    <div>
                        <!-- Next Button -->
                        <uui-button
                            look="secondary"
                            compact
                            @click=${this.#onNextClick}
                            ?disabled=${this.data!.uniques!.indexOf(this.value!.unique!) ===
                            this.data!.uniques!.length - 1}
                        >
                            <uui-icon name="icon-arrow-right"></uui-icon>
                        </uui-button>
                    </div>
                </div>
                <div id="main">
                    ${this._loading
                        ? html`<uui-loader></uui-loader>`
                        : html`
                              <table>
                                  <tr>
                                      <th>Timestamp</th>
                                      <td>
                                          <div>
                                              ${this._auditLog?.startTime
                                                  ? this.#formatTimestamp(this._auditLog?.startTime)
                                                  : "—"}
                                          </div>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Duration</th>
                                      <td>
                                          <div>
                                              ${this._auditLog?.durationMs
                                                  ? `${(this._auditLog?.durationMs / 1000).toFixed(2)}s`
                                                  : "—"}
                                          </div>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Status</th>
                                      <td>
                                          <div>${this.#renderStatusBadge(this._auditLog?.status ?? "Unknown")}</div>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>User</th>
                                      <td>
                                          <div>
                                              ${this._auditLog?.userName ?? this._auditLog?.userId ?? "Anonymous"}
                                          </div>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Feature</th>
                                      <td>
                                          <div>${this._auditLog?.featureType}</div>
                                          <small>
                                              ${this._auditLog?.featureId}${this._auditLog?.featureVersion
                                                  ? html`/v${this._auditLog?.featureVersion}`
                                                  : ""}
                                          </small>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Profile</th>
                                      <td>
                                          <div>${this._auditLog?.profileAlias}</div>
                                          <small>
                                              ${this._auditLog?.profileId}${this._auditLog?.profileVersion
                                                  ? html`/v${this._auditLog?.profileVersion}`
                                                  : ""}
                                          </small>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Provider</th>
                                      <td>
                                          <div>${this._auditLog?.providerId}</div>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Model</th>
                                      <td>
                                          <div>${this._auditLog?.modelId}</div>
                                      </td>
                                  </tr>
                                  <tr>
                                      <th>Tokens</th>
                                      <td>
                                          <div>
                                              ${this._auditLog?.inputTokens ?? 0} / ${this._auditLog?.outputTokens ?? 0}
                                          </div>
                                      </td>
                                  </tr>
                                  ${this._auditLog?.errorMessage
                                      ? html`
                                            <tr>
                                                <th>Error Message</th>
                                                <td>
                                                    <div>${this._auditLog?.errorMessage}</div>
                                                </td>
                                            </tr>
                                        `
                                      : ""}
                                  ${this._auditLog?.promptSnapshot
                                      ? html`
                                            <tr>
                                                <th>Prompt</th>
                                                <td>
                                                    <pre>${this._auditLog?.promptSnapshot}</pre>
                                                </td>
                                            </tr>
                                        `
                                      : ""}
                                  ${this._auditLog?.responseSnapshot
                                      ? html`
                                            <tr>
                                                <th>Response</th>
                                                <td>
                                                    <pre>${this._auditLog?.responseSnapshot}</pre>
                                                </td>
                                            </tr>
                                        `
                                      : ""}
                                  ${this._auditLog?.metadata
                                      ? html`
                                            <tr>
                                                <th>Metadata</th>
                                                <td>
                                                    ${repeat(
                                                        this._auditLog?.metadata ?? [],
                                                        (item) => item.key,
                                                        (item) =>
                                                            html`<div class="metadata-entry">
                                                                <div><strong>${item.key}:</strong></div>
                                                                <div><pre>${item.value ?? "—"}</pre></div>
                                                            </div> `,
                                                    )}
                                                </td>
                                            </tr>
                                        `
                                      : ""}
                              </table>
                          `}
                </div>
                <uui-button slot="actions" label=${this.localize.term("general_close")} @click=${this._rejectModal}>
                    ${this.localize.term("general_close")}
                </uui-button>
            </umb-body-layout>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            uui-loader {
                display: block;
                margin: var(--uui-size-space-4) auto;
            }

            .header-layout {
                display: flex;
                align-items: center;
                justify-content: space-between;
                gap: var(--uui-size-space-4);
                width: 100%;
            }

            .header-layout div:nth-child(2) {
                flex: 1 0 auto;
            }

            h3 {
                margin: 0;
            }

            h3 small {
                font-size: 12px;
                color: var(--uui-palette-dusty-grey-dark);
            }

            table {
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background-color: var(--uui-color-background);
                width: 100%;
            }

            th,
            td {
                text-align: left;
                vertical-align: top;
                padding: var(--uui-size-space-4);
                border-bottom: 1px solid var(--uui-color-border);
            }

            tr:last-child td,
            tr:last-child th {
                border-bottom: none;
            }

            pre {
                margin: 0;
                white-space: pre-wrap;
            }

            uui-button {
                user-select: none;
            }

            .metadata-entry {
                margin-bottom: var(--uui-size-space-4);
            }
            .metadata-entry:last-child {
                margin-bottom: 0;
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-audit-log-details-modal": UaiAuditLogDetailsModalElement;
    }
}

export default UaiAuditLogDetailsModalElement;

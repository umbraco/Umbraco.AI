import { customElement, property, css, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentToolStatus, UaiAgentToolElementProps } from "@umbraco-ai/agent-ui";
import type { SearchUmbracoResult, UmbracoSearchResultItem } from "./search-umbraco.types.js";

/**
 * Custom element for rendering Umbraco search results.
 * Displays content and media items with thumbnails, metadata, and expandable details.
 */
@customElement("uai-tool-search-umbraco")
export class UaiToolSearchUmbracoElement extends UmbLitElement implements UaiAgentToolElementProps {
    @property({ type: Object })
    args: Record<string, unknown> = {};

    @property({ type: String })
    status: UaiAgentToolStatus = "pending";

    @property({ type: Object })
    result?: SearchUmbracoResult;

    #getIcon(item: UmbracoSearchResultItem): string {
        if (item.type === "media") {
            return "icon-picture";
        }
        return "icon-document";
    }

    #renderLoading() {
        const query = (this.args.query as string) || "your query";
        return html`
            <div class="search-card loading">
                <div class="search-loading">
                    <uui-loader></uui-loader>
                    <span>Searching for "${query}"...</span>
                </div>
            </div>
        `;
    }

    #renderError() {
        return html`
            <div class="search-card error">
                <uui-icon name="icon-alert"></uui-icon>
                <span>Failed to search Umbraco</span>
            </div>
        `;
    }

    #renderEmpty() {
        const query = (this.args.query as string) || "";
        return html`
            <div class="search-card empty">
                <uui-icon name="icon-search"></uui-icon>
                <div class="empty-message">
                    <strong>No results found</strong>
                    <span>No content or media matching "${query}"</span>
                </div>
            </div>
        `;
    }

    #renderResultItem(item: UmbracoSearchResultItem) {
        if (item.type === "media") {
            return this.#renderMediaCard(item);
        } else {
            return this.#renderContentCard(item);
        }
    }

    #renderMediaCard(item: UmbracoSearchResultItem) {
        return html`
            <div class="result-item">
                <uui-card-media
                    name=${item.name}
                    disabled
                    style="--uui-color-contrast-disabled: var(--uui-color-text);"
                >
                    ${item.thumbnailUrl
                        ? html`<img src="${item.thumbnailUrl}" alt="${item.name}" />`
                        : html`<uui-icon name="${this.#getIcon(item)}"></uui-icon>`}
                    <div slot="tag">
                        <uui-tag color="default" look="secondary">${item.contentType}</uui-tag>
                    </div>
                </uui-card-media>
            </div>
        `;
    }

    #renderContentCard(item: UmbracoSearchResultItem) {
        return html`
            <div class="result-item">
                <uui-card-content-node
                    name=${item.name}
                    disabled
                    .selectable=${false}
                    style="--uui-color-contrast-disabled: var(--uui-color-text);"
                >
                    <ul style="list-style: none; padding-inline-start: 0px; margin: 0;">
                        <li><span style="font-weight: 700">Content Type:</span> ${item.contentType}</li>
                    </ul>
                </uui-card-content-node>
            </div>
        `;
    }

    #renderResults() {
        if (!this.result) return html``;
        return html`
            <div class="search-results">
                <div class="search-list">${this.result.results.map((item) => this.#renderResultItem(item))}</div>
            </div>
        `;
    }

    override render() {
        if (this.status === "error") {
            return this.#renderError();
        }

        if (this.status === "pending" || this.status === "executing" || this.status === "streaming") {
            return this.#renderLoading();
        }

        if (!this.result?.success || !this.result.results.length) {
            return this.#renderEmpty();
        }

        return this.#renderResults();
    }

    static override styles = css`
        :host {
            display: block;
        }

        .search-card {
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            padding: var(--uui-size-space-4);
            min-width: 300px;
            max-width: 700px;
        }

        .search-card.loading {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100px;
        }

        .search-card.error {
            background: var(--uui-color-danger-standalone);
            color: var(--uui-color-danger-contrast);
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
        }

        .search-card.empty {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--uui-size-space-3);
            padding: var(--uui-size-space-6);
        }

        .search-loading {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--uui-size-space-2);
            color: var(--uui-color-text-alt);
        }

        .empty-message {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: var(--uui-size-space-1);
            text-align: center;
        }

        .empty-message strong {
            color: var(--uui-color-text);
            font-size: var(--uui-type-default-size);
        }

        .empty-message span {
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .search-results {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-3);
            min-width: 300px;
            max-width: 700px;
        }

        .search-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            padding-bottom: var(--uui-size-space-3);
            border-bottom: 1px solid var(--uui-color-border);
        }

        .search-query-info {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
        }

        .search-query {
            font-weight: 600;
            color: var(--uui-color-text);
        }

        .search-count {
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
        }

        .search-list {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
            gap: var(--uui-size-space-4);
        }

        .result-item {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }

        uui-card-media {
            width: 100%;
            height: 200px;
        }

        uui-card-media img,
        uui-card-media uui-icon {
            width: 100%;
            height: 100%;
            object-fit: cover;
        }

        uui-card-media uui-icon {
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 48px;
            color: var(--uui-color-text-alt);
            background: var(--uui-color-surface-alt);
        }

        uui-card {
            width: 100%;
            height: auto;
            min-height: 80px;
        }

        .content-card-body {
            display: flex;
            gap: var(--uui-size-space-3);
            align-items: center;
            padding: var(--uui-size-space-4);
        }

        .content-icon {
            width: 48px;
            height: 48px;
            display: flex;
            align-items: center;
            justify-content: center;
            background: var(--uui-color-surface-alt);
            border-radius: var(--uui-border-radius);
            flex-shrink: 0;
        }

        .content-icon uui-icon {
            font-size: 24px;
            color: var(--uui-color-text-alt);
        }

        .content-info {
            flex: 1;
            min-width: 0;
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-1);
        }

        .content-name {
            font-weight: 600;
            color: var(--uui-color-text);
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .result-meta {
            display: flex;
            justify-content: space-between;
            align-items: center;
            gap: var(--uui-size-space-2);
            padding: 0 var(--uui-size-space-2);
        }

        .meta-info {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: var(--uui-size-space-2);
            font-size: var(--uui-type-small-size);
            flex: 1;
        }

        .result-date,
        .result-score {
            color: var(--uui-color-text-alt);
        }

        .result-toggle {
            padding: var(--uui-size-space-1);
            background: none;
            border: none;
            color: var(--uui-color-interactive);
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
        }

        .result-toggle:hover {
            color: var(--uui-color-interactive-emphasis);
        }

        .result-toggle uui-icon {
            font-size: 14px;
        }

        .result-details {
            margin-top: var(--uui-size-space-3);
            padding-top: var(--uui-size-space-3);
            border-top: 1px solid var(--uui-color-border);
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }

        .detail-row {
            display: flex;
            gap: var(--uui-size-space-2);
            font-size: var(--uui-type-small-size);
        }

        .detail-label {
            font-weight: 600;
            color: var(--uui-color-text);
            min-width: 50px;
        }

        .detail-value {
            color: var(--uui-color-text-alt);
            word-break: break-all;
        }

        .detail-url {
            color: var(--uui-color-interactive);
        }

        .detail-id {
            font-family: monospace;
            font-size: 11px;
        }
    `;
}

export default UaiToolSearchUmbracoElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-tool-search-umbraco": UaiToolSearchUmbracoElement;
    }
}

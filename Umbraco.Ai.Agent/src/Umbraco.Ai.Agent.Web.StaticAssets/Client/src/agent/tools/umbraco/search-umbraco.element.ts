import { customElement, property, css, html, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import type { UaiAgentToolStatus, UaiAgentToolElementProps } from "../uai-agent-tool.extension.js";
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

	@state()
	private _expandedItems: Set<string> = new Set();

	#getIcon(item: UmbracoSearchResultItem): string {
		if (item.Type === "media") {
			return "icon-picture";
		}
		return "icon-document";
	}

	#getTypeColor(type: string): string {
		return type === "media" ? "#3b82f6" : "#10b981";
	}

	#formatDate(dateString: string): string {
		try {
			const date = new Date(dateString);
			return date.toLocaleDateString();
		} catch {
			return dateString;
		}
	}

	#toggleExpanded(id: string) {
		if (this._expandedItems.has(id)) {
			this._expandedItems.delete(id);
		} else {
			this._expandedItems.add(id);
		}
		this.requestUpdate();
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
		const isExpanded = this._expandedItems.has(item.Id);

		return html`
			<div class="result-card" data-type="${item.Type}">
				<div class="result-main">
					${item.Type === "media" && item.ThumbnailUrl
						? html`<img class="result-thumbnail" src="${item.ThumbnailUrl}" alt="${item.Name}" />`
						: html`<div class="result-icon">
								<uui-icon name="${this.#getIcon(item)}"></uui-icon>
						  </div>`}

					<div class="result-content">
						<div class="result-title">
							${item.Url
								? html`<a href="${item.Url}" target="_blank" rel="noopener noreferrer"
										>${item.Name}</a
								  >`
								: html`<span>${item.Name}</span>`}
						</div>

						<div class="result-meta">
							<uui-tag color="default" look="primary">${item.ContentType}</uui-tag>
							<uui-tag
								style="--uui-tag-background: ${this.#getTypeColor(item.Type)}; --uui-tag-color: white;"
								>${item.Type}</uui-tag
							>
							<span class="result-date">${this.#formatDate(item.UpdateDate)}</span>
							<span class="result-score">Score: ${Math.round(item.Score * 100)}%</span>
						</div>

						${isExpanded ? this.#renderItemDetails(item) : ""}
					</div>

					<button class="result-toggle" @click=${() => this.#toggleExpanded(item.Id)}>
						<uui-icon name="${isExpanded ? "icon-arrow-up" : "icon-arrow-down"}"></uui-icon>
					</button>
				</div>
			</div>
		`;
	}

	#renderItemDetails(item: UmbracoSearchResultItem) {
		return html`
			<div class="result-details">
				<div class="detail-row">
					<span class="detail-label">Path:</span>
					<span class="detail-value">${item.Path}</span>
				</div>
				${item.Url
					? html`<div class="detail-row">
							<span class="detail-label">URL:</span>
							<span class="detail-value detail-url">${item.Url}</span>
					  </div>`
					: ""}
				<div class="detail-row">
					<span class="detail-label">ID:</span>
					<span class="detail-value detail-id">${item.Id}</span>
				</div>
			</div>
		`;
	}

	#renderResults() {
		if (!this.result) return html``;

		return html`
			<div class="search-results">
				<div class="search-header">
					<div class="search-query-info">
						<uui-icon name="icon-search"></uui-icon>
						<span class="search-query">"${this.args.query}"</span>
						<span class="search-count">${this.result.Results.length} result${this.result.Results
								.length === 1
								? ""
								: "s"}</span>
					</div>
				</div>
				<div class="search-list">${this.result.Results.map((item) => this.#renderResultItem(item))}</div>
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

		if (!this.result?.Success || !this.result.Results.length) {
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
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-2);
		}

		.result-card {
			background: var(--uui-color-surface);
			border: 1px solid var(--uui-color-border);
			border-radius: var(--uui-border-radius);
			padding: var(--uui-size-space-3);
			transition:
				border-color 0.2s,
				box-shadow 0.2s;
		}

		.result-card:hover {
			border-color: var(--uui-color-border-emphasis);
			box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
		}

		.result-main {
			display: flex;
			gap: var(--uui-size-space-3);
			align-items: flex-start;
		}

		.result-thumbnail {
			width: 80px;
			height: 80px;
			object-fit: cover;
			border-radius: var(--uui-border-radius);
			flex-shrink: 0;
		}

		.result-icon {
			width: 80px;
			height: 80px;
			display: flex;
			align-items: center;
			justify-content: center;
			background: var(--uui-color-surface-alt);
			border-radius: var(--uui-border-radius);
			flex-shrink: 0;
		}

		.result-icon uui-icon {
			font-size: 32px;
			color: var(--uui-color-text-alt);
		}

		.result-content {
			flex: 1;
			min-width: 0;
		}

		.result-title {
			font-size: var(--uui-type-default-size);
			font-weight: 600;
			margin-bottom: var(--uui-size-space-2);
		}

		.result-title a {
			color: var(--uui-color-interactive);
			text-decoration: none;
		}

		.result-title a:hover {
			text-decoration: underline;
		}

		.result-title span {
			color: var(--uui-color-text);
		}

		.result-meta {
			display: flex;
			flex-wrap: wrap;
			align-items: center;
			gap: var(--uui-size-space-2);
			font-size: var(--uui-type-small-size);
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

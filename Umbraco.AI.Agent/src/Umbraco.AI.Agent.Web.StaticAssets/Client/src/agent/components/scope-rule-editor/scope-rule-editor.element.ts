import { LitElement, html, css } from "lit";
import { customElement, property, state } from "lit/decorators.js";
import type { UaiAgentScopeRule } from "../../types.js";

@customElement("uai-agent-scope-rule-editor")
export class UaiAgentScopeRuleEditorElement extends LitElement {
	@property({ type: Object })
	rule?: UaiAgentScopeRule;

	@property({ type: Number })
	index: number = 0;

	@property({ type: String })
	ruleType: "allow" | "deny" = "allow";

	@state()
	private _isExpanded = false;

	/**
	 * Generates human-readable summary of rule constraints.
	 * Examples:
	 * - "Section: content AND Entity Type: document"
	 * - "Section: media"
	 * - "All contexts (no restrictions)"
	 */
	#getRuleSummary(): string {
		if (!this.rule) return "Empty rule";

		const parts: string[] = [];

		if (this.rule.sectionAliases?.length) {
			parts.push(`Section: ${this.rule.sectionAliases.join(", ")}`);
		}

		if (this.rule.entityTypeAliases?.length) {
			parts.push(`Entity Type: ${this.rule.entityTypeAliases.join(", ")}`);
		}

		if (this.rule.workspaceAliases?.length) {
			parts.push(`Workspace: ${this.rule.workspaceAliases.join(", ")}`);
		}

		return parts.length > 0 ? parts.join(" AND ") : "All contexts (no restrictions)";
	}

	#onSectionAliasesChange(e: CustomEvent) {
		this.dispatchEvent(
			new CustomEvent("section-aliases-change", {
				detail: {
					index: this.index,
					ruleType: this.ruleType,
					value: e.detail.value,
				},
				bubbles: true,
				composed: true,
			})
		);
	}

	#onEntityTypeAliasesChange(e: CustomEvent) {
		this.dispatchEvent(
			new CustomEvent("entity-type-aliases-change", {
				detail: {
					index: this.index,
					ruleType: this.ruleType,
					value: e.detail.value,
				},
				bubbles: true,
				composed: true,
			})
		);
	}

	#onWorkspaceAliasesChange(e: CustomEvent) {
		this.dispatchEvent(
			new CustomEvent("workspace-aliases-change", {
				detail: {
					index: this.index,
					ruleType: this.ruleType,
					value: e.detail.value,
				},
				bubbles: true,
				composed: true,
			})
		);
	}

	#onRemove() {
		this.dispatchEvent(
			new CustomEvent("remove-rule", {
				detail: { index: this.index, ruleType: this.ruleType },
				bubbles: true,
				composed: true,
			})
		);
	}

	#toggleExpanded() {
		this._isExpanded = !this._isExpanded;
	}

	render() {
		const ruleLabel = this.ruleType === "allow" ? "Allow Rule" : "Deny Rule";

		return html`
			<div class="rule-card">
				<div class="rule-header" @click=${this.#toggleExpanded}>
					<div class="rule-summary">
						<uui-symbol-expand .open=${this._isExpanded}></uui-symbol-expand>
						<strong>${ruleLabel} ${this.index + 1}:</strong>
						<span class="summary-text">${this.#getRuleSummary()}</span>
					</div>
					<uui-button
						compact
						look="secondary"
						label="Remove rule"
						@click=${(e: Event) => {
							e.stopPropagation();
							this.#onRemove();
						}}>
						<uui-icon name="icon-trash"></uui-icon>
					</uui-button>
				</div>

				${this._isExpanded
					? html`
							<div class="rule-content">
								<umb-property-layout
									label="Section Aliases"
									description="Section pathnames where this rule applies (e.g., 'content', 'media'). Leave empty for any section.">
									<umb-input-multi-text
										slot="editor"
										placeholder="content, media"
										.value=${this.rule?.sectionAliases ?? []}
										@change=${this.#onSectionAliasesChange}>
									</umb-input-multi-text>
								</umb-property-layout>

								<umb-property-layout
									label="Entity Type Aliases"
									description="Entity types where this rule applies (e.g., 'document', 'media'). Leave empty for any entity type.">
									<umb-input-multi-text
										slot="editor"
										placeholder="document, media"
										.value=${this.rule?.entityTypeAliases ?? []}
										@change=${this.#onEntityTypeAliasesChange}>
									</umb-input-multi-text>
								</umb-property-layout>

								<umb-property-layout
									label="Workspace Aliases"
									description="Workspace aliases where this rule applies (e.g., 'Umb.Workspace.Document'). Leave empty for any workspace.">
									<umb-input-multi-text
										slot="editor"
										placeholder="Umb.Workspace.Document"
										.value=${this.rule?.workspaceAliases ?? []}
										@change=${this.#onWorkspaceAliasesChange}>
									</umb-input-multi-text>
								</umb-property-layout>
							</div>
					  `
					: ""}
			</div>
		`;
	}

	static styles = css`
		:host {
			display: block;
		}

		.rule-card {
			border: 1px solid var(--uui-color-border);
			border-radius: var(--uui-border-radius);
			margin-bottom: var(--uui-size-space-4);
			background: var(--uui-color-surface);
		}

		.rule-header {
			display: flex;
			justify-content: space-between;
			align-items: center;
			padding: var(--uui-size-space-4);
			cursor: pointer;
			user-select: none;
		}

		.rule-header:hover {
			background: var(--uui-color-surface-emphasis);
		}

		.rule-summary {
			display: flex;
			align-items: center;
			gap: var(--uui-size-space-3);
			flex: 1;
		}

		.summary-text {
			color: var(--uui-color-text-alt);
			font-size: 0.9em;
		}

		.rule-header uui-button {
			opacity: 0;
			transition: opacity 0.2s;
		}

		.rule-card:hover .rule-header uui-button {
			opacity: 1;
		}

		.rule-content {
			padding: var(--uui-size-space-4);
			padding-top: 0;
			display: flex;
			flex-direction: column;
			gap: var(--uui-size-space-4);
		}
	`;
}

declare global {
	interface HTMLElementTagNameMap {
		"uai-agent-scope-rule-editor": UaiAgentScopeRuleEditorElement;
	}
}

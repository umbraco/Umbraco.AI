import { css, html, customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiAgentScopeRule } from "../../types.js";
import "../../../core/components/section-tags-input/section-tags-input.element.js";
import "../../../core/components/entity-type-tags-input/entity-type-tags-input.element.js";

/**
 * Creates an empty agent scope rule.
 */
export function createEmptyAgentScopeRule(): UaiAgentScopeRule {
	return {
		sectionAliases: null,
		entityTypeAliases: null,
		workspaceAliases: null,
	};
}

/**
 * Generates a human-readable summary for an agent scope rule.
 */
function getRuleSummary(rule: UaiAgentScopeRule): string {
	const parts: string[] = [];

	if (rule.sectionAliases && rule.sectionAliases.length > 0) {
		parts.push(`Section: ${rule.sectionAliases.join(" | ")}`);
	}

	if (rule.entityTypeAliases && rule.entityTypeAliases.length > 0) {
		parts.push(`Entity Type: ${rule.entityTypeAliases.join(" | ")}`);
	}

	if (rule.workspaceAliases && rule.workspaceAliases.length > 0) {
		parts.push(`Workspace: ${rule.workspaceAliases.join(" | ")}`);
	}

	if (parts.length === 0) {
		return "Matches all contexts";
	}

	return parts.join(" AND ");
}

/**
 * Individual agent scope rule editor with collapsible UI.
 *
 * @fires rule-change - Fires when the rule is modified
 * @fires remove - Fires when the remove button is clicked
 */
@customElement("uai-agent-scope-rule-editor")
export class UaiAgentScopeRuleEditorElement extends UmbLitElement {
	@property({ type: Object })
	rule: UaiAgentScopeRule = createEmptyAgentScopeRule();

	@state()
	private _collapsed = true;

	#toggleCollapsed() {
		this._collapsed = !this._collapsed;
	}

	#onSectionAliasesChange(event: Event) {
		event.stopPropagation();
		const target = event.target as HTMLElement & { items: string[] };
		const value = target.items;
		this.#dispatchChange({
			...this.rule,
			sectionAliases: value.length > 0 ? value : null,
		});
	}

	#onEntityTypeAliasesChange(event: Event) {
		event.stopPropagation();
		const target = event.target as HTMLElement & { items: string[] };
		const value = target.items;
		this.#dispatchChange({
			...this.rule,
			entityTypeAliases: value.length > 0 ? value : null,
		});
	}

	#onWorkspaceAliasesChange(event: CustomEvent) {
		event.stopPropagation();
		const value = event.detail.value as string[];
		this.#dispatchChange({
			...this.rule,
			workspaceAliases: value.length > 0 ? value : null,
		});
	}

	#onRemove(event: Event) {
		event.stopPropagation();
		this.dispatchEvent(new Event("remove", { bubbles: true, composed: true }));
	}

	#dispatchChange(rule: UaiAgentScopeRule) {
		this.dispatchEvent(
			new CustomEvent<UaiAgentScopeRule>("rule-change", {
				detail: rule,
				bubbles: true,
				composed: true,
			})
		);
	}

	render() {
		const summary = getRuleSummary(this.rule);

		return html`
			<div class="rule-card">
				<div class="rule-header" @click=${this.#toggleCollapsed} aria-expanded=${!this._collapsed}>
					<uui-symbol-expand ?open=${!this._collapsed}></uui-symbol-expand>
					<span class="rule-summary">${summary}</span>
					<uui-action-bar>
						<uui-button
							look="secondary"
							color="default"
							compact
							@click=${this.#onRemove}
							label="Remove rule"
						>
							<uui-icon name="icon-trash"></uui-icon>
						</uui-button>
					</uui-action-bar>
				</div>

				<div class="rule-content" ?hidden=${this._collapsed}>
					<umb-property-layout
						label="Section Aliases"
						description="Section pathnames where this rule applies (e.g., 'content', 'media'). Leave empty for any section."
						orientation="vertical"
					>
						<uai-section-tags-input
							slot="editor"
							.items=${this.rule.sectionAliases ?? []}
							@change=${this.#onSectionAliasesChange}
						></uai-section-tags-input>
					</umb-property-layout>

					<umb-property-layout
						label="Entity Type Aliases"
						description="Entity types where this rule applies (e.g., 'document', 'media'). Leave empty for any entity type."
						orientation="vertical"
					>
						<uai-entity-type-tags-input
							slot="editor"
							.items=${this.rule.entityTypeAliases ?? []}
							@change=${this.#onEntityTypeAliasesChange}
						></uai-entity-type-tags-input>
					</umb-property-layout>

					<umb-property-layout
						label="Workspace Aliases"
						description="Workspace aliases where this rule applies (e.g., 'Umb.Workspace.Document'). Leave empty for any workspace."
						orientation="vertical"
					>
						<umb-input-multi-text
							slot="editor"
							placeholder="Umb.Workspace.Document"
							.value=${this.rule.workspaceAliases ?? []}
							@change=${this.#onWorkspaceAliasesChange}
						></umb-input-multi-text>
					</umb-property-layout>
				</div>
			</div>
		`;
	}

	static styles = [
		UmbTextStyles,
		css`
			:host {
				display: block;
				--umb-scope-rule-entry-actions-opacity: 0;
			}

			:host(:hover),
			:host(:focus-within) {
				--umb-scope-rule-entry-actions-opacity: 1;
			}

			uui-action-bar {
				opacity: var(--umb-scope-rule-entry-actions-opacity, 0);
				transition: opacity 120ms;
			}

			.rule-header {
				display: flex;
				align-items: center;
				gap: var(--uui-size-space-3);
				padding: var(--uui-size-space-1) var(--uui-size-space-4);
				background: transparent;
				cursor: pointer;
				text-align: left;
				font: inherit;
				color: inherit;
				border: 1px solid var(--uui-color-border);
			}

			.rule-header:hover,
			.rule-header:focus {
				border-color: var(--uui-color-border-emphasis);
			}

			.rule-summary {
				flex: 1;
				color: var(--uui-color-text-alt);
				overflow: hidden;
				text-overflow: ellipsis;
				white-space: nowrap;
				line-height: 1;
			}

			.rule-content {
				padding: var(--uui-size-space-5);
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-6);
				border: 1px solid var(--uui-color-border);
				border-top: 0;
			}

			.rule-content[hidden] {
				display: none;
			}

			.rule-content umb-property-layout {
				--uui-size-layout-1: 0;
			}

			uui-symbol-expand {
				flex-shrink: 0;
			}
		`,
	];
}

export default UaiAgentScopeRuleEditorElement;

declare global {
	interface HTMLElementTagNameMap {
		"uai-agent-scope-rule-editor": UaiAgentScopeRuleEditorElement;
	}
}

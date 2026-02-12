import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiAgentScopeRule } from "../../types.js";
import { createEmptyAgentScopeRule } from "./scope-rule-editor.element.js";

/**
 * Editor for managing a list of agent scope rules.
 * Handles adding, removing, and updating rules.
 *
 * @fires rules-change - Fires when the rules array changes
 */
@customElement("uai-agent-scope-rules-editor")
export class UaiAgentScopeRulesEditorElement extends UmbLitElement {
	@property({ type: Array })
	rules: UaiAgentScopeRule[] = [];

	@property({ type: String })
	addButtonLabel = "Add Rule";

	#onAddRule() {
		const newRules = [...this.rules, createEmptyAgentScopeRule()];
		this.#dispatchChange(newRules);
	}

	#onRemoveRule(index: number) {
		const newRules = this.rules.filter((_, i) => i !== index);
		this.#dispatchChange(newRules);
	}

	#onRuleChange(index: number, rule: UaiAgentScopeRule) {
		const newRules = [...this.rules];
		newRules[index] = rule;
		this.#dispatchChange(newRules);
	}

	#dispatchChange(rules: UaiAgentScopeRule[]) {
		this.dispatchEvent(
			new CustomEvent<UaiAgentScopeRule[]>("rules-change", {
				detail: rules,
				bubbles: true,
				composed: true,
			})
		);
	}

	render() {
		return html`
			<div class="rules-container">
				${this.rules.map(
					(rule, index) => html`
						<uai-agent-scope-rule-editor
							.rule=${rule}
							@rule-change=${(e: CustomEvent<UaiAgentScopeRule>) => this.#onRuleChange(index, e.detail)}
							@remove=${() => this.#onRemoveRule(index)}
						></uai-agent-scope-rule-editor>
					`
				)}
				<uui-button look="placeholder" @click=${this.#onAddRule}>
					<uui-icon name="icon-add"></uui-icon>
					${this.addButtonLabel}
				</uui-button>
			</div>
		`;
	}

	static styles = [
		UmbTextStyles,
		css`
			:host {
				display: block;
			}

			.rules-container {
				display: flex;
				flex-direction: column;
				gap: var(--uui-size-space-3);
			}

			.rules-container uui-button[look="placeholder"] {
				width: 100%;
			}
		`,
	];
}

export default UaiAgentScopeRulesEditorElement;

declare global {
	interface HTMLElementTagNameMap {
		"uai-agent-scope-rules-editor": UaiAgentScopeRulesEditorElement;
	}
}

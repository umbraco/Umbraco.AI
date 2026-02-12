import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiScopeRule } from "../../property-actions/types.js";
import { createEmptyRule } from "./prompt-scope-rule-editor.element.js";

/**
 * Editor for managing a list of prompt scope rules (allow or deny).
 * Handles adding, removing, and updating rules.
 *
 * @fires rules-change - Fires when the rules array changes
 */
@customElement("uai-prompt-scope-rules-editor")
export class UaiPromptScopeRulesEditorElement extends UmbLitElement {
    @property({ type: Array })
    rules: UaiScopeRule[] = [];

    @property({ type: String })
    addButtonLabel = "Add Rule";

    #onAddRule() {
        const newRules = [...this.rules, createEmptyRule()];
        this.#dispatchChange(newRules);
    }

    #onRemoveRule(index: number) {
        const newRules = this.rules.filter((_, i) => i !== index);
        this.#dispatchChange(newRules);
    }

    #onRuleChange(index: number, rule: UaiScopeRule) {
        const newRules = [...this.rules];
        newRules[index] = rule;
        this.#dispatchChange(newRules);
    }

    #dispatchChange(rules: UaiScopeRule[]) {
        this.dispatchEvent(
            new CustomEvent<UaiScopeRule[]>("rules-change", {
                detail: rules,
                bubbles: true,
                composed: true,
            }),
        );
    }

    render() {
        return html`
            <div class="rules-container">
                ${this.rules.map(
                    (rule, index) => html`
                        <uai-prompt-scope-rule-editor
                            .rule=${rule}
                            @rule-change=${(e: CustomEvent<UaiScopeRule>) => this.#onRuleChange(index, e.detail)}
                            @remove=${() => this.#onRemoveRule(index)}
                        ></uai-prompt-scope-rule-editor>
                    `,
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

export default UaiPromptScopeRulesEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-prompt-scope-rules-editor": UaiPromptScopeRulesEditorElement;
    }
}

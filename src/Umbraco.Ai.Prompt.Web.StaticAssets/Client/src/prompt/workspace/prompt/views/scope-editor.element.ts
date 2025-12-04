import { css, html, customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiPromptScope, UaiScopeRule } from "../../../property-actions/types.js";
import { TEXT_BASED_PROPERTY_EDITOR_UIS } from "../../../property-actions/constants.js";

/**
 * Event dispatched when the scope changes.
 */
export class UaiScopeChangeEvent extends Event {
    static readonly TYPE = "scope-change";

    constructor(public readonly scope: UaiPromptScope | null) {
        super(UaiScopeChangeEvent.TYPE, { bubbles: true, composed: true });
    }
}

/**
 * Creates an empty scope rule.
 */
function createEmptyRule(): UaiScopeRule {
    return {
        propertyEditorUiAliases: null,
        propertyAliases: null,
        documentTypeAliases: null,
    };
}

/**
 * Creates a default scope with one include rule for all text-based editors.
 */
function createDefaultScope(): UaiPromptScope {
    return {
        includeRules: [{
            propertyEditorUiAliases: [...TEXT_BASED_PROPERTY_EDITOR_UIS],
            propertyAliases: null,
            documentTypeAliases: null,
        }],
        excludeRules: [],
    };
}

/**
 * Scope editor element for configuring where a prompt appears.
 */
@customElement("uai-scope-editor")
export class UaiScopeEditorElement extends UmbLitElement {
    @property({ type: Object })
    scope: UaiPromptScope | null = null;

    @state()
    private _localScope: UaiPromptScope = createDefaultScope();

    updated(changedProperties: Map<string, unknown>) {
        if (changedProperties.has("scope")) {
            // Sync local scope with incoming scope property
            this._localScope = this.scope ?? createDefaultScope();
        }
    }

    #onAddIncludeRule() {
        this._localScope = {
            ...this._localScope,
            includeRules: [...this._localScope.includeRules, createEmptyRule()],
        };
        this.#dispatchChange(this._localScope);
    }

    #onAddExcludeRule() {
        this._localScope = {
            ...this._localScope,
            excludeRules: [...this._localScope.excludeRules, createEmptyRule()],
        };
        this.#dispatchChange(this._localScope);
    }

    #onRemoveIncludeRule(index: number) {
        this._localScope = {
            ...this._localScope,
            includeRules: this._localScope.includeRules.filter((_, i) => i !== index),
        };
        this.#dispatchChange(this._localScope);
    }

    #onRemoveExcludeRule(index: number) {
        this._localScope = {
            ...this._localScope,
            excludeRules: this._localScope.excludeRules.filter((_, i) => i !== index),
        };
        this.#dispatchChange(this._localScope);
    }

    #onIncludeRuleChange(index: number, rule: UaiScopeRule) {
        const newRules = [...this._localScope.includeRules];
        newRules[index] = rule;
        this._localScope = {
            ...this._localScope,
            includeRules: newRules,
        };
        this.#dispatchChange(this._localScope);
    }

    #onExcludeRuleChange(index: number, rule: UaiScopeRule) {
        const newRules = [...this._localScope.excludeRules];
        newRules[index] = rule;
        this._localScope = {
            ...this._localScope,
            excludeRules: newRules,
        };
        this.#dispatchChange(this._localScope);
    }

    #dispatchChange(scope: UaiPromptScope | null) {
        this.dispatchEvent(new UaiScopeChangeEvent(scope));
    }

    render() {
        return html`
            ${this.#renderScopeConfig()}
        `;
    }

    #renderScopeConfig() {
        return html`
            <div class="scope-config">
                <div class="rules-section">
                    <div class="section-header">
                        <h4>Include Rules</h4>
                        <small>Prompt appears where ANY rule matches (OR logic between rules)</small>
                    </div>
                    ${this._localScope.includeRules.map((rule, index) => html`
                        <uai-scope-rule-editor
                            .rule=${rule}
                            @rule-change=${(e: CustomEvent<UaiScopeRule>) => this.#onIncludeRuleChange(index, e.detail)}
                            @remove=${() => this.#onRemoveIncludeRule(index)}
                        ></uai-scope-rule-editor>
                    `)}
                    <uui-button
                        look="placeholder"
                        @click=${this.#onAddIncludeRule}
                    >
                        <uui-icon name="icon-add"></uui-icon>
                        Add Include Rule
                    </uui-button>
                </div>

                <div class="rules-section exclude">
                    <div class="section-header">
                        <h4>Exclude Rules</h4>
                        <small>Prompt is hidden where ANY rule matches (overrides includes)</small>
                    </div>
                    ${this._localScope.excludeRules.map((rule, index) => html`
                        <uai-scope-rule-editor
                            .rule=${rule}
                            @rule-change=${(e: CustomEvent<UaiScopeRule>) => this.#onExcludeRuleChange(index, e.detail)}
                            @remove=${() => this.#onRemoveExcludeRule(index)}
                        ></uai-scope-rule-editor>
                    `)}
                    <uui-button
                        look="placeholder"
                        @click=${this.#onAddExcludeRule}
                    >
                        <uui-icon name="icon-add"></uui-icon>
                        Add Exclude Rule
                    </uui-button>
                </div>
            </div>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .scope-header {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-4);
                margin-bottom: var(--uui-size-space-4);
            }

            .scope-status {
                display: flex;
                align-items: center;
            }

            .disabled-message {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-4);
                background: var(--uui-color-warning-standalone);
                border-radius: var(--uui-border-radius);
                color: var(--uui-color-warning-contrast);
            }

            .disabled-message p {
                margin: 0;
            }

            .scope-config {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            .rules-section {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
            }

            .section-header {
                margin-bottom: var(--uui-size-space-2);
            }

            .section-header h4 {
                margin: 0 0 var(--uui-size-space-1) 0;
                font-size: var(--uui-type-default-size);
                font-weight: 600;
            }

            .section-header small {
                color: var(--uui-color-text-alt);
            }

            .rules-section.exclude .section-header h4 {
                color: var(--uui-color-danger);
            }

            uui-button[look="placeholder"] {
                width: 100%;
            }
        `,
    ];
}

/**
 * Individual scope rule editor.
 */
@customElement("uai-scope-rule-editor")
export class UaiScopeRuleEditorElement extends UmbLitElement {
    @property({ type: Object })
    rule: UaiScopeRule = createEmptyRule();

    #onPropertyEditorUisChange(event: Event) {
        const value = (event.target as HTMLInputElement).value;
        const aliases = value ? value.split(",").map(s => s.trim()).filter(Boolean) : null;
        this.#dispatchChange({
            ...this.rule,
            propertyEditorUiAliases: aliases && aliases.length > 0 ? aliases : null,
        });
    }

    #onPropertyAliasesChange(event: Event) {
        const value = (event.target as HTMLInputElement).value;
        const aliases = value ? value.split(",").map(s => s.trim()).filter(Boolean) : null;
        this.#dispatchChange({
            ...this.rule,
            propertyAliases: aliases && aliases.length > 0 ? aliases : null,
        });
    }

    #onDocumentTypeAliasesChange(event: Event) {
        const value = (event.target as HTMLInputElement).value;
        const aliases = value ? value.split(",").map(s => s.trim()).filter(Boolean) : null;
        this.#dispatchChange({
            ...this.rule,
            documentTypeAliases: aliases && aliases.length > 0 ? aliases : null,
        });
    }

    #onRemove() {
        this.dispatchEvent(new Event("remove", { bubbles: true, composed: true }));
    }

    #dispatchChange(rule: UaiScopeRule) {
        this.dispatchEvent(new CustomEvent<UaiScopeRule>("rule-change", {
            detail: rule,
            bubbles: true,
            composed: true,
        }));
    }

    render() {
        return html`
            <div class="rule-card">
                <div class="rule-header">
                    <span class="rule-title">Rule</span>
                    <uui-button
                        look="secondary"
                        color="danger"
                        compact
                        @click=${this.#onRemove}
                    >
                        <uui-icon name="icon-trash"></uui-icon>
                    </uui-button>
                </div>
                <div class="rule-fields">
                    <umb-property-layout
                        label="Property Editor UIs"
                        description="Comma-separated list (e.g., Umb.PropertyEditorUi.TextBox). Empty = any."
                        orientation="vertical"
                    >
                        <uui-input
                            slot="editor"
                            .value=${this.rule.propertyEditorUiAliases?.join(", ") ?? ""}
                            @input=${this.#onPropertyEditorUisChange}
                            placeholder="Any property editor"
                        ></uui-input>
                    </umb-property-layout>

                    <umb-property-layout
                        label="Property Aliases"
                        description="Comma-separated list (e.g., title, subtitle). Empty = any."
                        orientation="vertical"
                    >
                        <uui-input
                            slot="editor"
                            .value=${this.rule.propertyAliases?.join(", ") ?? ""}
                            @input=${this.#onPropertyAliasesChange}
                            placeholder="Any property"
                        ></uui-input>
                    </umb-property-layout>

                    <umb-property-layout
                        label="Document Type Aliases"
                        description="Comma-separated list (e.g., article, blogPost). Empty = any."
                        orientation="vertical"
                    >
                        <uui-input
                            slot="editor"
                            .value=${this.rule.documentTypeAliases?.join(", ") ?? ""}
                            @input=${this.#onDocumentTypeAliasesChange}
                            placeholder="Any document type"
                        ></uui-input>
                    </umb-property-layout>
                </div>
                <div class="rule-summary">
                    ${this.#renderRuleSummary()}
                </div>
            </div>
        `;
    }

    #renderRuleSummary() {
        const parts: string[] = [];

        if (this.rule.documentTypeAliases && this.rule.documentTypeAliases.length > 0) {
            parts.push(`Doc type: ${this.rule.documentTypeAliases.join(" OR ")}`);
        }
        if (this.rule.propertyAliases && this.rule.propertyAliases.length > 0) {
            parts.push(`Property: ${this.rule.propertyAliases.join(" OR ")}`);
        }
        if (this.rule.propertyEditorUiAliases && this.rule.propertyEditorUiAliases.length > 0) {
            const simplified = this.rule.propertyEditorUiAliases.map(a => a.replace("Umb.PropertyEditorUi.", ""));
            parts.push(`Editor: ${simplified.join(" OR ")}`);
        }

        if (parts.length === 0) {
            return html`<em>Matches everything</em>`;
        }

        return html`<code>${parts.join(" AND ")}</code>`;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .rule-card {
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                padding: var(--uui-size-space-4);
                background: var(--uui-color-surface);
            }

            .rule-header {
                display: flex;
                justify-content: space-between;
                align-items: center;
                margin-bottom: var(--uui-size-space-3);
            }

            .rule-title {
                font-weight: 600;
            }

            .rule-fields {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-2);
            }

            .rule-fields umb-property-layout {
                --uui-size-layout-1: 0;
            }

            .rule-fields uui-input {
                width: 100%;
            }

            .rule-summary {
                margin-top: var(--uui-size-space-3);
                padding-top: var(--uui-size-space-3);
                border-top: 1px solid var(--uui-color-border);
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
            }

            .rule-summary code {
                background: var(--uui-color-surface-emphasis);
                padding: var(--uui-size-space-1) var(--uui-size-space-2);
                border-radius: var(--uui-border-radius);
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-scope-editor": UaiScopeEditorElement;
        "uai-scope-rule-editor": UaiScopeRuleEditorElement;
    }
}

import {
    css,
    customElement,
    html,
    property,
    repeat,
    when,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiVersionPropertyChange } from "../../types.js";

/**
 * A component that displays property changes between two versions
 * with visual highlighting for additions, deletions, and modifications.
 *
 * @element uai-version-diff-view
 */
@customElement("uai-version-diff-view")
export class UaiVersionDiffViewElement extends UmbLitElement {
    /**
     * The list of property changes to display.
     */
    @property({ attribute: false })
    changes: UaiVersionPropertyChange[] = [];

    override render() {
        if (this.changes.length === 0) {
            return html`
                <p class="no-changes">
                    ${this.localize.term("uaiVersionHistory_noChanges")}
                </p>
            `;
        }

        return html`
            <div class="diff-container">
                ${repeat(
                    this.changes,
                    (change) => change.propertyName,
                    (change) => this.#renderChange(change)
                )}
            </div>
        `;
    }

    #renderChange(change: UaiVersionPropertyChange) {
        const isAddition = !change.oldValue && change.newValue;
        const isDeletion = change.oldValue && !change.newValue;

        return html`
            <div class="change-item">
                <div class="property-name">${change.propertyName}</div>
                <div class="values">
                    ${when(
                        change.oldValue,
                        () => html`
                            <div class="value old-value ${isDeletion ? 'deletion' : ''}">
                                <span class="value-label">${this.localize.term("uaiVersionHistory_oldValue")}:</span>
                                ${this.#renderValue(change.oldValue)}
                            </div>
                        `
                    )}
                    ${when(
                        change.newValue,
                        () => html`
                            <div class="value new-value ${isAddition ? 'addition' : ''}">
                                <span class="value-label">${this.localize.term("uaiVersionHistory_newValue")}:</span>
                                ${this.#renderValue(change.newValue)}
                            </div>
                        `
                    )}
                </div>
            </div>
        `;
    }

    #renderValue(value?: string) {
        if (!value) return html`<span class="empty-value">-</span>`;

        // Check if it looks like JSON
        if (value.startsWith("{") || value.startsWith("[")) {
            try {
                const formatted = JSON.stringify(JSON.parse(value), null, 2);
                return html`<pre class="json-value">${formatted}</pre>`;
            } catch {
                // Not valid JSON, render as-is
            }
        }

        // For long text, use pre to preserve formatting
        if (value.length > 100 || value.includes("\n")) {
            return html`<pre class="text-value">${value}</pre>`;
        }

        return html`<span class="text-value">${value}</span>`;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .no-changes {
                text-align: center;
                color: var(--uui-color-text-alt);
                padding: var(--uui-size-space-5);
                margin: 0;
            }

            .diff-container {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }

            .change-item {
                border: 1px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                overflow: hidden;
            }

            .property-name {
                background: var(--uui-color-surface-alt);
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
                font-weight: 600;
                border-bottom: 1px solid var(--uui-color-border);
            }

            .values {
                display: flex;
                flex-direction: column;
            }

            .value {
                padding: var(--uui-size-space-3) var(--uui-size-space-4);
            }

            .value + .value {
                border-top: 1px solid var(--uui-color-border);
            }

            .value-label {
                font-size: 0.85em;
                color: var(--uui-color-text-alt);
                display: block;
                margin-bottom: var(--uui-size-space-2);
            }

            .old-value {
                background: rgba(255, 53, 53, 0.1);
            }

            .old-value.deletion {
                background: rgba(255, 53, 53, 0.2);
            }

            .old-value .text-value,
            .old-value .json-value {
                text-decoration: line-through;
                color: var(--uui-color-danger);
            }

            .new-value {
                background: rgba(0, 196, 62, 0.1);
            }

            .new-value.addition {
                background: rgba(0, 196, 62, 0.2);
            }

            .new-value .text-value,
            .new-value .json-value {
                color: var(--uui-color-positive);
            }

            .text-value {
                word-break: break-word;
            }

            .json-value {
                margin: 0;
                font-family: monospace;
                font-size: 0.9em;
                white-space: pre-wrap;
                word-break: break-word;
            }

            pre {
                background: var(--uui-color-surface-alt);
                padding: var(--uui-size-space-2);
                border-radius: var(--uui-border-radius);
                max-height: 200px;
                overflow: auto;
            }

            .empty-value {
                color: var(--uui-color-text-alt);
                font-style: italic;
            }
        `,
    ];
}

export default UaiVersionDiffViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-version-diff-view": UaiVersionDiffViewElement;
    }
}

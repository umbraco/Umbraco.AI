import {
    css,
    customElement,
    html,
    property,
    repeat,
    type TemplateResult,
} from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiVersionPropertyChange } from "../../types.js";
import { diffWords, type Change } from "diff";

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
                    <uui-icon name="info"></uui-icon>
                    ${this.localize.term("uaiVersionHistory_noChanges")}
                </p>
            `;
        }

        return html`
            <uui-table>
                <uui-table-column style="width: 0"></uui-table-column>
                <uui-table-column></uui-table-column>
                
                <uui-table-head>
                    <uui-table-head-cell>${this.localize.term('general_alias')}</uui-table-head-cell>
                    <uui-table-head-cell>${this.localize.term('general_value')}</uui-table-head-cell>
                </uui-table-head>
                
                ${repeat(
                        this.changes,
                        (change) => change.propertyName,
                        (change) => this.#renderChange(change)
                )}
            </uui-table>
        `;
    }

    #renderChange(change: UaiVersionPropertyChange) {
        const isAddition = !change.oldValue && change.newValue;
        const isDeletion = change.oldValue && !change.newValue;

        return html`
            <uui-table-row>
                <uui-table-cell>${change.propertyName}</uui-table-cell>
                <uui-table-cell>
                    <div class="diff-container ${isAddition ? 'addition' : ''} ${isDeletion ? 'deletion' : ''}">
                        ${this.#renderInlineDiff(change.oldValue, change.newValue)}
                    </div>
                </uui-table-cell>
            </uui-table-row>
        `;
    }

    #renderInlineDiff(oldValue?: string | null, newValue?: string | null): TemplateResult {
        // Handle pure addition (no old value)
        if (!oldValue && newValue) {
            return this.#renderFormattedValue(newValue, "added");
        }

        // Handle pure deletion (no new value)
        if (oldValue && !newValue) {
            return this.#renderFormattedValue(oldValue, "removed");
        }

        // Both values exist - compute diff
        if (oldValue && newValue) {
            // Format JSON for better diffing
            const formattedOld = this.#formatForDiff(oldValue);
            const formattedNew = this.#formatForDiff(newValue);

            const changes = diffWords(formattedOld, formattedNew);
            const isMultiline = formattedOld.includes("\n") || formattedNew.includes("\n");

            return html`
                <pre class="diff-value ${isMultiline ? 'multiline' : ''}">${changes.map(
                    (part) => this.#renderDiffPart(part)
                )}</pre>
            `;
        }

        return html`<span class="empty-value">-</span>`;
    }

    #formatForDiff(value: string): string {
        // Try to format JSON for cleaner diffs
        if (value.startsWith("{") || value.startsWith("[")) {
            try {
                return JSON.stringify(JSON.parse(value), null, 2);
            } catch {
                // Not valid JSON, return as-is
            }
        }
        return value;
    }

    #renderDiffPart(part: Change): TemplateResult {
        if (part.added) {
            return html`<span class="diff-added">${part.value}</span>`;
        }
        if (part.removed) {
            return html`<span class="diff-removed">${part.value}</span>`;
        }
        return html`<span class="diff-unchanged">${part.value}</span>`;
    }

    #renderFormattedValue(value: string, type: "added" | "removed"): TemplateResult {
        const cssClass = type === "added" ? "diff-added" : "diff-removed";
        const formatted = this.#formatForDiff(value);
        const isMultiline = formatted.includes("\n") || formatted.length > 100;

        if (isMultiline) {
            return html`<pre class="diff-value multiline"><span class="${cssClass}">${formatted}</span></pre>`;
        }

        return html`<span class="diff-value ${cssClass}">${formatted}</span>`;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .no-changes {
                display: flex;
                flex-direction: column;
                align-items: center;
                text-align: center;
                border: 1px solid var(--uui-color-border);
                color: var(--uui-color-text-alt);
                padding: var(--uui-size-space-6) var(--uui-size-space-5);
                border-radius: var(--uui-border-radius);
                margin: var(--uui-size-space-5) 0 0;
            }

            .no-changes uui-icon {
                color: var(--uui-color-text-alt);
                font-size: var(--uui-size-10);
                margin-bottom: var(--uui-size-space-3);
            }

            uui-table {
                --uui-table-cell-padding: var(--uui-size-space-1) var(--uui-size-space-4);
                margin-top: var(--uui-size-space-5);
            }
            uui-table-head-cell:first-child {
                border-top-left-radius: var(--uui-border-radius);
            }
            uui-table-head-cell:last-child {
                border-top-right-radius: var(--uui-border-radius);
            }
            uui-table-head-cell {
                background-color: var(--uui-color-surface-alt);
            }
            uui-table-head-cell:last-child,
            uui-table-cell:last-child {
                border-right: 1px solid var(--uui-color-border);
            }
            uui-table-head-cell,
            uui-table-cell {
                border-top: 1px solid var(--uui-color-border);
                border-left: 1px solid var(--uui-color-border);
            }
            uui-table-row:last-child uui-table-cell {
                border-bottom: 1px solid var(--uui-color-border);
            }
            uui-table-row:last-child uui-table-cell:last-child {
                border-bottom-right-radius: var(--uui-border-radius);
            }
            uui-table-row:last-child uui-table-cell:first-child {
                border-bottom-left-radius: var(--uui-border-radius);
            }

            /* Diff container */
            .diff-container {
                padding: var(--uui-size-space-2) var(--uui-size-space-1);
            }

            .diff-container.addition {
                background: rgba(0, 196, 62, 0.08);
            }

            .diff-container.deletion {
                background: rgba(255, 53, 53, 0.08);
            }

            /* Diff value display */
            .diff-value {
                font-family: var(--uui-font-family);
                font-size: var(--uui-type-default-size);
                line-height: 1.5;
                word-break: break-word;
                white-space: pre-wrap;
                margin: 0;
            }

            .diff-value.multiline {
                font-family: monospace;
                font-size: 0.9em;
                background: var(--uui-color-surface-alt);
                padding: var(--uui-size-space-2);
                border-radius: var(--uui-border-radius);
                max-height: 300px;
                overflow: auto;
            }

            /* Inline diff highlighting */
            .diff-added {
                background-color: rgba(0, 196, 62, 0.25);
                color: var(--uui-color-positive-emphasis);
                border-radius: 2px;
                padding: 0 1px;
            }

            .diff-removed {
                background-color: rgba(255, 53, 53, 0.25);
                color: var(--uui-color-danger-emphasis);
                text-decoration: line-through;
                border-radius: 2px;
                padding: 0 1px;
            }

            .diff-unchanged {
                color: var(--uui-color-text);
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

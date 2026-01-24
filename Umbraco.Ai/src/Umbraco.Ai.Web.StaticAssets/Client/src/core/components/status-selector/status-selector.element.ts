import { css, html, customElement, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

@customElement("uai-status-selector")
export class UaiStatusSelectorElement extends UmbLitElement {
    @property({ type: Boolean })
    value = true;

    @property({ type: String, attribute: "active-label" })
    activeLabel = "Active";

    @property({ type: String, attribute: "inactive-label" })
    inactiveLabel = "Inactive";

    @property({ type: String, attribute: "active-icon" })
    activeIcon = "icon-check";

    @property({ type: String, attribute: "inactive-icon" })
    inactiveIcon = "icon-block";

    #onSelect(isActive: boolean) {
        if (this.value === isActive) return;

        this.value = isActive;
        this.dispatchEvent(
            new CustomEvent("change", {
                detail: { value: isActive },
                bubbles: true,
                composed: true,
            })
        );
    }

    override render() {
        return html`
            <div id="container">
                <button
                    class=${this.value ? "selected" : ""}
                    @click=${() => this.#onSelect(true)}
                    aria-pressed=${this.value}
                >
                    <uui-icon name=${this.activeIcon}></uui-icon>
                    ${this.activeLabel}
                </button>
                <button
                    class=${!this.value ? "selected" : ""}
                    @click=${() => this.#onSelect(false)}
                    aria-pressed=${!this.value}
                >
                    <uui-icon name=${this.inactiveIcon}></uui-icon>
                    ${this.inactiveLabel}
                </button>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: inline-block;
            }

            #container {
                display: flex;
                border-radius: var(--uui-border-radius);
                border: 1px solid var(--uui-color-border);
                background: var(--uui-color-surface-alt);
                padding: 3px;
                gap: 2px;
            }

            button {
                display: flex;
                align-items: center;
                gap: var(--uui-size-space-2);
                padding: var(--uui-size-space-2) var(--uui-size-space-4);
                border: none;
                border-radius: calc(var(--uui-border-radius) - 2px);
                background: transparent;
                color: #aaa;
                font-family: inherit;
                font-size: var(--uui-type-small-size);
                font-weight: 500;
                cursor: pointer;
                transition: all 120ms ease;
                white-space: nowrap;
            }

            button:hover:not(.selected) {
                color: var(--uui-color-text);
                background: var(--uui-color-surface);
            }

            button.selected {
                background: var(--uui-color-surface);
                color: var(--uui-color-text);
                box-shadow: 0 1px 2px rgba(0, 0, 0, 0.08);
            }

            button uui-icon {
                font-size: 12px;
            }

            button.selected:first-child uui-icon {
                color: var(--uui-color-positive);
            }
            button.selected:last-child uui-icon {
                color: var(--uui-color-danger);
            }
        `,
    ];
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-status-selector": UaiStatusSelectorElement;
    }
}

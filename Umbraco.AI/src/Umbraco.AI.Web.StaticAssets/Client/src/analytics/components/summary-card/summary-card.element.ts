import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

@customElement("uai-analytics-summary-card")
export class UaiAnalyticsSummaryCardElement extends UmbLitElement {
    @property({ type: String })
    icon: string = "icon-activity";

    @property({ type: String })
    value: string = "";

    @property({ type: String })
    label: string = "";

    /**
     * Optional smaller addition on the value line, for a figure that only means something next to the main
     * value (e.g. how much of an input token total was served from cache).
     *
     * Rendered inline rather than as its own line so a card carrying one stays the same height as the rest
     * of the row - a taller card in a grid of otherwise equal cards reads as a layout fault.
     */
    @property({ type: String })
    valueSuffix?: string;

    constructor() {
        super();
    }

    render() {
        return html`<uui-card class="summary-card">
            <div class="card-icon"><uui-icon .name=${this.icon}></uui-icon></div>
            <div class="card-content">
                <div class="card-value">
                    ${this.value}${this.valueSuffix
                        ? html`<span class="card-value-suffix"> / ${this.valueSuffix}</span>`
                        : null}
                </div>
                <div class="card-label">${this.label}</div>
            </div>
        </uui-card>`;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
            }

            .summary-card {
                display: flex;
                gap: var(--uui-size-space-2);
                padding: var(--uui-size-space-5);
            }

            .card-icon {
                display: flex;
                align-items: center;
                justify-content: center;
                width: 32px;
                height: 32px;
            }

            .card-icon uui-icon {
                font-size: 1.5rem;
                color: var(--uui-color-current);
            }

            .card-content {
                flex: 1;
                display: flex;
                flex-direction: column;
            }

            .card-value {
                font-size: var(--uui-type-h3-size);
                font-weight: 700;
                line-height: 1;
            }

            .card-value-suffix {
                font-size: var(--uui-type-default-size);
                font-weight: 700;
                /* Colour is left to inherit from .card-value, so the suffix tracks the main value if that
                   ever changes. Kept on one line because "14.3k cached" is a single phrase, and wrapping
                   it would defeat the point of moving it onto the value line. */
                white-space: nowrap;
            }

            .card-label {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
                font-weight: 500;
            }

            .card-detail {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
            }
        `,
    ];
}

export default UaiAnalyticsSummaryCardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-summary-card": UaiAnalyticsSummaryCardElement;
    }
}

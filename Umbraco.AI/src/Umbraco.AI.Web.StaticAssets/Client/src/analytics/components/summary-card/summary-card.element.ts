
import { css, customElement, html, property } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

@customElement("uai-analytics-summary-card")
export class UaiAnalyticsSummaryCardElement extends UmbLitElement {

    @property({ type: String })
    icon: string = 'icon-activity';
    
    @property({ type: String })
    value: string = '';

    @property({ type: String })
    label: string = '';
    
    constructor() {
        super();
    }

    render() {
        return html`<uui-card class="summary-card">
            <div class="card-icon"><uui-icon .name=${this.icon}></uui-icon></div>
            <div class="card-content">
                <div class="card-value">${this.value}</div>
                <div class="card-label">${this.label}</div>
            </div>
        </uui-card>`
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



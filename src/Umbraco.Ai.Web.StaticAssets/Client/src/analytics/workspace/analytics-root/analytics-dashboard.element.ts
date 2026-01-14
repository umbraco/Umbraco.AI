import { css, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";

@customElement("uai-analytics-dashboard")
export class UaiAnalyticsDashboardElement extends UmbLitElement {

    constructor() {
        super();
    }

    override render() {
        return html`
            <div class="dashboard-container">
                <uui-box headline="AI Usage Analytics">
                    <div class="placeholder">
                        <div class="placeholder-icon">
                            <uui-icon name="icon-chart"></uui-icon>
                        </div>
                        <h3>Analytics Dashboard</h3>
                        <p>AI usage analytics dashboard will be displayed here.</p>
                        <p class="info">
                            Track token consumption, request patterns, and performance metrics across your AI integrations.
                        </p>
                    </div>
                </uui-box>
            </div>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                height: 100%;
                padding: var(--uui-size-space-6);
            }

            .dashboard-container {
                height: 100%;
            }

            uui-box {
                height: 100%;
            }

            .placeholder {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                padding: var(--uui-size-space-6);
                text-align: center;
                min-height: 400px;
            }

            .placeholder-icon {
                margin-bottom: var(--uui-size-space-5);
            }

            .placeholder-icon uui-icon {
                font-size: 4rem;
                color: var(--uui-color-border-emphasis);
            }

            .placeholder h3 {
                margin: 0 0 var(--uui-size-space-3);
                font-size: var(--uui-type-h4-size);
                font-weight: 600;
            }

            .placeholder p {
                margin: var(--uui-size-space-2) 0;
                color: var(--uui-color-text-alt);
            }

            .placeholder .info {
                max-width: 500px;
                font-size: var(--uui-type-small-size);
            }
        `,
    ];
}

export default UaiAnalyticsDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-dashboard": UaiAnalyticsDashboardElement;
    }
}

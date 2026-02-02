import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import '../../usage/components/usage-dashboard/usage-dashboard.element.js';

@customElement("uai-analytics-dashboard")
export class UaiAnalyticsDashboardElement extends UmbLitElement {

    override render() {
        return html`<uai-usage-dashboard></uai-usage-dashboard>`;
    }
}

export default UaiAnalyticsDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-analytics-dashboard": UaiAnalyticsDashboardElement;
    }
}

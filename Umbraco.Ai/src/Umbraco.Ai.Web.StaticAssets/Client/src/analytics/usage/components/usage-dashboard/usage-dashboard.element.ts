import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { customElement } from "@umbraco-cms/backoffice/external/lit";


@customElement("uai-usage-dashboard")
export class UaiUsageDashboardElement extends UmbLitElement {

    constructor() {
        super();
    }
    
    render() {
        
    }
}

export default UaiUsageDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-usage-dashboard": UaiUsageDashboardElement;
    }
}

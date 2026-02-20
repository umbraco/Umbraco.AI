import { html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type { UaiTestRunDetailModalData, UaiTestRunDetailModalValue } from "./test-run-detail-modal.token.js";

// Ensure the test-run-detail component is loaded
import "../../components/test-run-detail/test-run-detail.element.js";

/**
 * Sidebar modal that wraps the test run detail component.
 */
@customElement("uai-test-run-detail-modal")
export class UaiTestRunDetailModalElement extends UmbModalBaseElement<
    UaiTestRunDetailModalData,
    UaiTestRunDetailModalValue
> {
    render() {
        return html`
            <umb-body-layout headline="Test Run Detail">
                <uai-test-run-detail
                    .runId=${this.data?.runId}
                ></uai-test-run-detail>
                <div slot="actions">
                    <uui-button
                        label="Close"
                        @click=${this._rejectModal}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }
}

export default UaiTestRunDetailModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-detail-modal": UaiTestRunDetailModalElement;
    }
}

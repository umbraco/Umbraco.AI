import { html, css, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type { UaiTestRunDetailModalData, UaiTestRunDetailModalValue } from "./test-run-detail-modal.token.js";

// Ensure components are loaded
import "../../components/test-run-detail/test-run-detail.element.js";
import "../../components/test-run-transcript/test-run-transcript.element.js";
import "../../components/test-run-comparison/test-run-comparison.element.js";

/**
 * Sidebar modal that wraps the test run detail, transcript, and comparison components with tabs.
 */
@customElement("uai-test-run-detail-modal")
export class UaiTestRunDetailModalElement extends UmbModalBaseElement<
    UaiTestRunDetailModalData,
    UaiTestRunDetailModalValue
> {
    @state()
    private _activeTab: 'details' | 'transcript' | 'comparison' = 'details';

    #hasComparison(): boolean {
        return !!this.data?.baselineRunId
            && this.data.baselineRunId !== this.data.runId;
    }

    render() {
        return html`
            <umb-body-layout>
                <div slot="header" style="display: flex; align-items: center; gap: 20px;">
                    <h3 id="headline" title="Test Run Detail">Test Run Detail</h3>
                    ${this.data?.baselineRunId && this.data.baselineRunId === this.data.runId
                        ? html`<uui-tag color="default" look="outline">
                            <uui-icon name="icon-flag"></uui-icon>
                            This is the baseline run
                        </uui-tag>`
                        : ''}
                </div>
                <uui-tab-group slot="navigation">
                    <uui-tab
                        label="Details"
                        ?active=${this._activeTab === 'details'}
                        @click=${() => { this._activeTab = 'details'; }}
                    >
                        <uui-icon slot="icon" name="icon-info"></uui-icon>
                        Details
                    </uui-tab>
                    <uui-tab
                        label="Transcript"
                        ?active=${this._activeTab === 'transcript'}
                        @click=${() => { this._activeTab = 'transcript'; }}
                    >
                        <uui-icon slot="icon" name="icon-chat"></uui-icon>
                        Transcript
                    </uui-tab>
                    ${this.#hasComparison()
                        ? html`
                            <uui-tab
                                label="Compare"
                                ?active=${this._activeTab === 'comparison'}
                                @click=${() => { this._activeTab = 'comparison'; }}
                            >
                                <uui-icon slot="icon" name="icon-split"></uui-icon>
                                Compare
                            </uui-tab>
                        `
                        : ''}
                </uui-tab-group>

                ${this._activeTab === 'details'
                    ? html`
                        <uai-test-run-detail
                            .runId=${this.data?.runId}
                        ></uai-test-run-detail>
                    `
                    : this._activeTab === 'transcript'
                    ? html`
                        <uai-test-run-transcript
                            .runId=${this.data?.runId}
                        ></uai-test-run-transcript>
                    `
                    : html`
                        <uai-test-run-comparison
                            .baselineRunId=${this.data?.baselineRunId}
                            .comparisonRunId=${this.data?.runId}
                        ></uai-test-run-comparison>
                    `}

                <div slot="actions">
                    <uui-button
                        label="Close"
                        @click=${this._rejectModal}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = css`
        uui-tab-group {
            --uui-tab-divider: var(--uui-color-border);
            border-bottom: 1px solid var(--uui-color-border);
            border-left: 1px solid var(--uui-color-border);
        }
    `;
}

export default UaiTestRunDetailModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-run-detail-modal": UaiTestRunDetailModalElement;
    }
}

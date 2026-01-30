import { html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type { UaiTestDetailModel } from "../../../types.js";
import type { UaiTestWorkspaceContext } from "../test-workspace.context.js";
import { UAI_TEST_WORKSPACE_ALIAS } from "../../../constants.js";

/**
 * Workspace view for editing test details.
 *
 * TODO: This is a minimal placeholder. Needs to be enhanced with:
 * - Property editors for all test fields
 * - Test case configuration UI
 * - Grader configuration UI
 * - Validation and error handling
 */
@customElement("uai-test-details-workspace-view")
export class UaiTestDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: UaiTestWorkspaceContext;

    @state()
    private _test?: UaiTestDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_TEST_WORKSPACE_ALIAS, (context) => {
            this.#workspaceContext = context;
            this.observe(context.model, (model) => {
                this._test = model;
            });
        });
    }

    render() {
        if (!this._test) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <div>
                <uui-box headline="Test Configuration">
                    <p>Test workspace view - TODO: Implement property editors</p>
                    <p>Test ID: ${this._test.unique}</p>
                    <p>Name: ${this._test.name}</p>
                    <p>Alias: ${this._test.alias}</p>
                    <p>Test Type: ${this._test.testTypeId}</p>
                </uui-box>
            </div>
        `;
    }

    static styles = [UmbTextStyles];
}

export default UaiTestDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-test-details-workspace-view": UaiTestDetailsWorkspaceViewElement;
    }
}

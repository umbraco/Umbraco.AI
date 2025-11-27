import { css, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UaiConnectionRootWorkspaceContext } from "./connection-root-workspace.context.js";
import { UaiConnectionConstants } from "../constants.js";

@customElement("uai-connection-root-workspace")
export class UaiConnectionRootWorkspaceElement extends UmbLitElement {
    // Context is needed to provide workspace context to child components
    // @ts-ignore - Context is used by registering with host
    #workspaceContext = new UaiConnectionRootWorkspaceContext(this);

    render() {
        return html`
            <umb-workspace-editor alias="${UaiConnectionConstants.Workspace.Root}" headline="Connections">
                <umb-collection alias="${UaiConnectionConstants.Collection}"></umb-collection>
            </umb-workspace-editor>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                width: 100%;
                height: 100%;
            }
        `,
    ];
}

export default UaiConnectionRootWorkspaceElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-connection-root-workspace": UaiConnectionRootWorkspaceElement;
    }
}

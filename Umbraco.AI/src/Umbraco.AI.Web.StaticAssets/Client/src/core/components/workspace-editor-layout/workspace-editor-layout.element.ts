import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { customElement, html, css } from "@umbraco-cms/backoffice/external/lit";

@customElement("uai-workspace-editor-layout")
export class UaiWorkspaceEditorLayoutElement extends UmbLitElement {
    render = () =>
        html` <slot name="header"></slot>
            <div id="main">
                <div id="left-column"><slot></slot></div>
                <div id="right-column"><slot name="aside"></slot></div>
            </div>`;

    static styles = [
        css`
            :host {
                display: block;
            }

            #main {
                display: grid;
                grid-template-columns: 1fr 350px;
                gap: var(--uui-size-layout-1);
            }

            #left-column {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }
        `,
    ];
}

export default UaiWorkspaceEditorLayoutElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-workspace-editor-layout": UaiWorkspaceEditorLayoutElement;
    }
}

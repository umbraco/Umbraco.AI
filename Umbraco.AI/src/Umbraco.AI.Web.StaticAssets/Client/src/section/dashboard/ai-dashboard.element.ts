import {css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UAI_CONNECTION_ROOT_WORKSPACE_PATH } from "../../connection/workspace/connection-root/paths.js";

@customElement('uai-welcome-dashboard')
export class UaiWelcomeDashboardElement extends UmbLitElement {

    constructor() {
        super();
    }

    render() {
        return html`<div class="uui-text">
            <div class="container">
                <uui-icon class="main-icon" name="icon-bot"></uui-icon>
                <h1 class="uui-h2" style="margin-top: 0;">Welcome to Umbraco AI</h1>
                <p class="intro">Manage your AI integrations, profiles, and settings. Use the menu on the left to get started with connections, configure profiles, or explore add-ons like Prompts and Agents.</p>
                <uui-button look="primary" color="positive" label="Create a Connection" href=${UAI_CONNECTION_ROOT_WORKSPACE_PATH}>
                    Create a Connection
                </uui-button>
            </div>
        </div>
        `;

    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                box-sizing: border-box;
                padding: var(--uui-size-layout-3); /* --uui-size-space-6) */;
                height: 100%;
                overflow-y: auto;
            }

            .container {
                max-width: 600px;
                margin: 0 auto;
                text-align: center;
            }

            .intro {
                font-size: var(--uui-size-5);
                color: var(--uui-color-text-secondary);
            }

            .main-icon {
                color: var(--uui-color-primary);
                width: 128px;
                height: 128px;
                margin-bottom: var(--uui-size-space-4);
            }

            uui-button {
                margin-top: var(--uui-size-space-5);
            }
		`,
    ];

}

export default UaiWelcomeDashboardElement;

declare global {
    interface HTMLElementTagNameMap {
        'uai-welcome-dashboard': UaiWelcomeDashboardElement;
    }
}

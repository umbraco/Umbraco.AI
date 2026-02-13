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
                <svg class="logo" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 750 750"><defs><style>.cls-2{fill:#283a97}</style></defs><path id="YOpwF5.tif" d="M527.1 318.22c-27.18 8.82-52.77 20.31-77.04 36.41-1.79 1.19-4.15 1.13-5.84-.2-56.97-44.76-95.18-109.48-106.95-180.61-4.23-25.59 15.56-48.86 41.5-48.87 20.55 0 38.14 14.82 41.5 35.1 10.44 63 49.66 118.2 107.65 149.05 3.87 2.06 3.35 7.77-.82 9.13Z" style="fill:#f5c1bc"/><path id="YOpwF5.tif-2" d="M318.26 222.87c8.82 27.18 20.31 52.77 36.41 77.04 1.19 1.79 1.13 4.15-.2 5.84-44.76 56.97-109.48 95.18-180.61 106.95-25.59 4.23-48.86-15.56-48.87-41.5 0-20.55 14.82-38.14 35.1-41.5 63-10.44 118.2-49.66 149.05-107.65 2.06-3.87 7.77-3.35 9.13.82Z" class="cls-2" data-name="YOpwF5.tif"/><path id="YOpwF5.tif-3" d="M431.74 527.08c-8.82-27.18-20.31-52.77-36.41-77.04-1.19-1.79-1.13-4.15.2-5.84 44.76-56.97 109.48-95.18 180.61-106.95 25.59-4.23 48.86 15.56 48.87 41.5 0 20.55-14.82 38.14-35.1 41.5-63 10.44-118.2 49.66-149.05 107.65-2.06 3.87-7.77 3.35-9.13-.82Z" class="cls-2" data-name="YOpwF5.tif"/><path id="YOpwF5.tif-4" d="M222.87 431.73c27.18-8.82 52.77-20.31 77.04-36.41 1.79-1.19 4.15-1.13 5.84.2C362.72 440.28 400.93 505 412.7 576.13c4.23 25.59-15.56 48.86-41.5 48.87-20.55 0-38.14-14.82-41.5-35.1-10.44-63-49.66-118.2-107.65-149.05-3.87-2.06-3.35-7.77.82-9.13Z" class="cls-2" data-name="YOpwF5.tif"/></svg>
                <h1 class="uui-h2" style="margin-top: 0;">Welcome to Umbraco AI</h1>
                <p class="intro">Bring the power of AI into your content management workflow with seamless integrations to leading providers.</p>
                <p>Use the menu on the left to get started with connections, configure profiles, or explore add-ons like Prompts and Agents.</p>
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

            .uui-text .intro {
                font-size: var(--uui-size-6);
                color: var(--uui-color-text-secondary);
                font-weight: 300;
                margin-bottom: var(--uui-size-layout-2);
            }

            .logo {
                width: 140px;
                height: 140px;
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

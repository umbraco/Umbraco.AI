import { customElement, property, state, css, html, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type { ManifestUaiAgentToolRenderer } from "../extensions/uai-agent-tool-renderer.extension.js";
import type { UaiInterruptInfo } from "../types/index.js";
import type { UaiApprovalBaseConfig } from "./approval-base.element.js";

/**
 * HITL (Human-in-the-Loop) approval element for interrupt handling.
 *
 * Uses uai-approval-base internally for the actual UI rendering.
 *
 * @element uai-hitl-approval
 * @fires respond - Dispatched with JSON-serialized response string
 */
@customElement("uai-hitl-approval")
export class UaiHitlApprovalElement extends UmbLitElement {
    @property({ type: Object })
    interrupt?: UaiInterruptInfo;

    @state()
    private _baseConfig?: UaiApprovalBaseConfig;

    override updated(changedProperties: Map<string, unknown>) {
        super.updated(changedProperties);

        if (changedProperties.has("interrupt") && this.interrupt) {
            this.#buildBaseConfig();
        }
    }

    #buildBaseConfig() {
        if (!this.interrupt) return;

        let elementAlias = "Uai.AgentApprovalElement.Default";
        let config: Record<string, unknown> = {};
        const args: Record<string, unknown> = this.interrupt.payload ?? {
            title: this.interrupt.title,
            message: this.interrupt.message,
            options: this.interrupt.options,
        };

        // Check if interrupt payload has a toolName - look up renderer manifest for approval config
        const toolName = this.interrupt.payload?.toolName as string | undefined;
        if (toolName) {
            const toolManifests = umbExtensionsRegistry.getByTypeAndFilter<
                "uaiAgentToolRenderer",
                ManifestUaiAgentToolRenderer
            >("uaiAgentToolRenderer", (m) => m.meta.toolName === toolName);

            if (toolManifests.length > 0 && toolManifests[0].meta.approval) {
                const approval = toolManifests[0].meta.approval;
                const isSimple = approval === true;
                const approvalObj = typeof approval === "object" ? approval : null;
                elementAlias = isSimple
                    ? "Uai.AgentApprovalElement.Default"
                    : (approvalObj?.elementAlias ?? "Uai.AgentApprovalElement.Default");
                config = isSimple ? {} : (approvalObj?.config ?? {});
            }
        } else {
            elementAlias = "Uai.AgentApprovalElement.Default";
            config = {
                title: this.interrupt.title,
                message: this.interrupt.message,
            };
            if (this.interrupt.options?.length) {
                const approveOpt = this.interrupt.options.find((o) => o.variant === "positive" || o.value === "yes");
                const denyOpt = this.interrupt.options.find((o) => o.variant === "danger" || o.value === "no");
                if (approveOpt) config.approveLabel = approveOpt.label;
                if (denyOpt) config.denyLabel = denyOpt.label;
            }
        }

        this._baseConfig = {
            elementAlias,
            config,
            args,
        };
    }

    #handleResponse = (response: unknown) => {
        const jsonResponse = JSON.stringify(response);

        this.dispatchEvent(
            new CustomEvent("respond", {
                detail: jsonResponse,
                bubbles: true,
                composed: true,
            }),
        );
    };

    override render() {
        if (!this.interrupt) {
            return nothing;
        }

        if (!this._baseConfig) {
            return html`
                <div class="interrupt-card">
                    <div class="loading">
                        <uui-loader></uui-loader>
                    </div>
                </div>
            `;
        }

        return html`
            <div class="interrupt-card">
                <uai-approval-base .config=${this._baseConfig} .onResponse=${this.#handleResponse}></uai-approval-base>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
            margin-top: var(--uui-size-space-2);
        }

        .interrupt-card {
            display: inline-block;
            margin: 0 var(--uui-size-space-3);
            padding: var(--uui-size-space-3);
            background: var(--uui-color-surface-alt);
            border-radius: var(--uui-border-radius);
        }

        .loading {
            display: flex;
            justify-content: center;
            padding: var(--uui-size-space-4);
        }
    `;
}

export default UaiHitlApprovalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-hitl-approval": UaiHitlApprovalElement;
    }
}

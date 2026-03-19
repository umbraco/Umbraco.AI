import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import type { UaiProfileDetailModel, UaiChatProfileSettings } from "../../../types.js";
import { isChatSettings } from "../../../types.js";
import { UaiPartialUpdateCommand } from "../../../../core/index.js";
import { UAI_PROFILE_WORKSPACE_CONTEXT } from "../profile-workspace.context-token.js";

/**
 * Workspace view for Profile governance settings.
 * Displays guardrail picker for chat profiles.
 */
@customElement("uai-profile-governance-workspace-view")
export class UaiProfileGovernanceWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_PROFILE_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiProfileDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_PROFILE_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onGuardrailIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#updateChatSettings({ guardrailIds: picker.value });
    }

    #updateChatSettings(updates: Partial<UaiChatProfileSettings>) {
        const currentSettings = this._model?.settings ?? null;
        const chatSettings: UaiChatProfileSettings = isChatSettings(currentSettings)
            ? { ...currentSettings, ...updates }
            : {
                $type: "chat",
                temperature: updates.temperature ?? null,
                maxTokens: updates.maxTokens ?? null,
                systemPromptTemplate: updates.systemPromptTemplate ?? null,
                contextIds: updates.contextIds ?? [],
                guardrailIds: updates.guardrailIds ?? [],
            };

        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiProfileDetailModel>({ settings: chatSettings }, "settings"),
        );
    }

    #getChatSettings(): UaiChatProfileSettings | null {
        return isChatSettings(this._model?.settings ?? null) ? (this._model!.settings as UaiChatProfileSettings) : null;
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        const chatSettings = this.#getChatSettings();

        return html`
            <uui-box headline="Guardrails">
                <umb-property-layout label="Guardrails" description="Guardrails to evaluate inputs and responses">
                    <uai-guardrail-picker
                        slot="editor"
                        multiple
                        .value=${chatSettings?.guardrailIds}
                        @change=${this.#onGuardrailIdsChange}
                    ></uai-guardrail-picker>
                </umb-property-layout>
            </uui-box>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            :host {
                display: block;
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-loader {
                display: block;
                margin: auto;
                position: absolute;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
            }
        `,
    ];
}

export default UaiProfileGovernanceWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-profile-governance-workspace-view": UaiProfileGovernanceWorkspaceViewElement;
    }
}

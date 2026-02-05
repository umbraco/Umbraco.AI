import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand } from "@umbraco-ai/core";
import type { UaiAgentDetailModel } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

import "@umbraco-cms/backoffice/markdown-editor";

/**
 * Workspace view for Agent details.
 * Displays system prompt, description, profile, and contexts.
 */
@customElement("uai-agent-details-workspace-view")
export class UaiAgentDetailsWorkspaceViewElement extends UmbLitElement {
    #workspaceContext?: typeof UAI_AGENT_WORKSPACE_CONTEXT.TYPE;

    @state()
    private _model?: UaiAgentDetailModel;

    constructor() {
        super();
        this.consumeContext(UAI_AGENT_WORKSPACE_CONTEXT, (context) => {
            if (context) {
                this.#workspaceContext = context;
                this.observe(context.model, (model) => {
                    this._model = model;
                });
            }
        });
    }

    #onDescriptionChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ description: value || null }, "description")
        );
    }

    #onInstructionsChange(event: Event) {
        event.stopPropagation();
        const value = (event.target as HTMLInputElement).value;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ instructions: value || null }, "instructions")
        );
    }

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? null;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ profileId }, "profileId")
        );
    }

    #onContextIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { contextIds: picker.value ?? [] },
                "contextIds"
            )
        );
    }

    #onScopeIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { scopeIds: picker.value ?? [] },
                "scopeIds"
            )
        );
    }

    #onEnabledToolScopeIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { allowedToolScopeIds: picker.value ?? [] },
                "allowedToolScopeIds"
            )
        );
    }

    #onEnabledToolIdsChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string[] | undefined };
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>(
                { allowedToolIds: picker.value ?? [] },
                "allowedToolIds"
            )
        );
    }

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">
                <umb-property-layout label="AI Profile" description="Select a profile or leave empty to use the default Chat profile from Settings">
                    <uai-profile-picker
                        slot="editor"
                        .value=${this._model.profileId || undefined}
                        @change=${this.#onProfileChange}
                    ></uai-profile-picker>
                </umb-property-layout>

                <umb-property-layout label="Description" description="Brief description of this agent">
                    <uui-input
                        slot="editor"
                        .value=${this._model.description ?? ""}
                        @input=${this.#onDescriptionChange}
                        placeholder="Enter description..."
                    ></uui-input>
                </umb-property-layout>

                <umb-property-layout label="Contexts" description="Predefined contexts to include when running this agent">
                    <uai-context-picker
                        slot="editor"
                        multiple
                        .value=${this._model.contextIds}
                        @change=${this.#onContextIdsChange}
                    ></uai-context-picker>
                </umb-property-layout>

                <umb-property-layout label="Instructions" description="Instructions that define how this agent behaves">
                    <umb-input-markdown
                        slot="editor"
                        .value=${this._model.instructions ?? ""}
                        @change=${this.#onInstructionsChange}
                    ></umb-input-markdown>
                </umb-property-layout>
            </uui-box>
            <uui-box headline="Scope">
                <umb-property-layout label="Scopes" description="Select how this agent can be used (e.g., Copilot chat)">
                    <uai-agent-scope-picker
                            slot="editor"
                            multiple
                            .value=${this._model.scopeIds}
                            @change=${this.#onScopeIdsChange}
                    ></uai-agent-scope-picker>
                </umb-property-layout>
            </uui-box>
            <uui-box headline="Tool Permissions">
                <umb-property-layout
                    label="Allowed Tool Scopes"
                    description="Select which tool scopes this agent can access. Tools belonging to these scopes will be enabled automatically. System tools are always available.">
                    <uai-tool-scope-permissions
                        slot="editor"
                        multiple
                        .value=${this._model.allowedToolScopeIds}
                        @change=${this.#onEnabledToolScopeIdsChange}
                    ></uai-tool-scope-permissions>
                </umb-property-layout>
                <umb-property-layout
                    label="Allowed Tools"
                    description="Select specific tools to enable for this agent beyond those included in the selected scopes. System tools are always available.">
                    <uai-tool-picker
                        slot="editor"
                        .value=${this._model.allowedToolIds}
                        @change=${this.#onEnabledToolIdsChange}
                    ></uai-tool-picker>
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
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            uui-input {
                width: 100%;
            }

            umb-input-markdown {
                width: 100%;
                --umb-code-editor-height: 400px;
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

export default UaiAgentDetailsWorkspaceViewElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-agent-details-workspace-view": UaiAgentDetailsWorkspaceViewElement;
    }
}

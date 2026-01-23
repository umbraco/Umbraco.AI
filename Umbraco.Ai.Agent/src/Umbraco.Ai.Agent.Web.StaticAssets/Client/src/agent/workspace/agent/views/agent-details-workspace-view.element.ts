import { css, html, customElement, state, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UaiPartialUpdateCommand, UAI_EMPTY_GUID, formatDateTime } from "@umbraco-ai/core";
import type { UaiAgentDetailModel } from "../../../types.js";
import { UAI_AGENT_WORKSPACE_CONTEXT } from "../agent-workspace.context-token.js";

import "@umbraco-cms/backoffice/markdown-editor";

/**
 * Workspace view for Agent details.
 * Displays system prompt, description, profile, and status.
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

    #onIsActiveChange(event: Event) {
        event.stopPropagation();
        const checked = (event.target as HTMLInputElement).checked;
        this.#workspaceContext?.handleCommand(
            new UaiPartialUpdateCommand<UaiAgentDetailModel>({ isActive: checked }, "isActive")
        );
    }

    #onProfileChange(event: UmbChangeEvent) {
        event.stopPropagation();
        const picker = event.target as HTMLElement & { value: string | undefined };
        const profileId = picker.value ?? "";
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

    render() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <div class="layout">
                <div class="main-column">${this.#renderLeftColumn()}</div>
                <div class="aside-column">${this.#renderRightColumn()}</div>
            </div>
        `;
    }

    #renderLeftColumn() {
        if (!this._model) return html`<uui-loader></uui-loader>`;

        return html`
            <uui-box headline="General">
                <umb-property-layout label="AI Profile" description="The AI profile this agent uses for model configuration">
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

            ${this._model.unique && this._model.unique !== UAI_EMPTY_GUID ? html`
                <uai-version-history-table
                    entity-type="agent"
                    entity-id=${this._model.unique}
                    @rollback=${() => this.#workspaceContext?.reload()}>
                </uai-version-history-table>
            ` : nothing}
        `;
    }

    #renderRightColumn() {
        if (!this._model) return null;

        return html`
            <uui-box headline="Info">
                <umb-property-layout label="Id" orientation="vertical">
                    <div slot="editor">${this._model.unique === UAI_EMPTY_GUID
                        ? html`<uui-tag color="default" look="placeholder">Unsaved</uui-tag>`
                        : this._model.unique}</div>
                </umb-property-layout>
                ${this._model.dateCreated ? html`
                    <umb-property-layout label="Date Created" orientation="vertical">
                        <div slot="editor">${formatDateTime(this._model.dateCreated)}</div>
                    </umb-property-layout>
                ` : ''}
                ${this._model.dateModified ? html`
                    <umb-property-layout label="Date Modified" orientation="vertical">
                        <div slot="editor">${formatDateTime(this._model.dateModified)}</div>
                    </umb-property-layout>
                ` : ''}
                <umb-property-layout label="Active" orientation="vertical">
                    <uui-toggle
                        slot="editor"
                        ?checked=${this._model.isActive}
                        @change=${this.#onIsActiveChange}
                    ></uui-toggle>
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

            .layout {
                display: grid;
                grid-template-columns: 1fr 350px;
                gap: var(--uui-size-layout-1);
            }

            .main-column {
                min-width: 0;
            }

            .aside-column {
                min-width: 0;
            }

            @media (max-width: 1024px) {
                .layout {
                    grid-template-columns: 1fr;
                }
            }

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }
            uui-box:not(:first-child),
            uai-version-history-table {
                margin-top: var(--uui-size-layout-1);
            }

            uui-input {
                width: 100%;
            }

            umb-input-markdown {
                width: 100%;
                --umb-code-editor-height: 400px;
            }

            umb-property-layout[orientation="vertical"]:not(:last-child) {
                padding-bottom: 0;
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

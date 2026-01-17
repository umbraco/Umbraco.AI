import {
    css,
    customElement,
    html,
    state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import type { UaiContextResourceInjectionMode } from '../../types.js';
import type {
    UaiResourceOptionsModalData,
    UaiResourceOptionsModalValue,
    UaiResourceOptionsData,
} from './resource-options-modal.token.js';
import type { UaiModelEditorChangeEventDetail } from '../../../core/components/exports.js';

const elementName = 'uai-resource-options-modal';

@customElement(elementName)
export class UaiResourceOptionsModalElement extends UmbModalBaseElement<
    UaiResourceOptionsModalData,
    UaiResourceOptionsModalValue
> {
    @state()
    private _resourceName = '';

    @state()
    private _resourceDescription = '';

    @state()
    private _resourceData: Record<string, unknown> = {};

    @state()
    private _injectionMode: UaiContextResourceInjectionMode = 'Always';

    override connectedCallback() {
        super.connectedCallback();

        // Populate from existing resource if editing
        if (this.data?.resource) {
            this._resourceName = this.data.resource.name;
            this._resourceDescription = this.data.resource.description ?? '';
            this._resourceData = this.data.resource.data ?? {};
            this._injectionMode = this.data.resource.injectionMode;
        } else {
            // Default name from resource type
            this._resourceName = this.data?.resourceType?.name ?? 'New Resource';
        }
    }

    #handleSubmit() {
        if (!this._resourceName.trim()) return;

        const resource: UaiResourceOptionsData = {
            name: this._resourceName.trim(),
            description: this._resourceDescription.trim() || null,
            data: this._resourceData,
            injectionMode: this._injectionMode,
        };

        this.modalContext?.setValue({ resource });
        this.modalContext?.submit();
    }

    #handleCancel() {
        this.modalContext?.reject();
    }

    #onNameChange(e: Event) {
        this._resourceName = (e.target as HTMLInputElement).value;
    }

    #onDescriptionChange(e: Event) {
        this._resourceDescription = (e.target as HTMLInputElement).value;
    }

    #onDataChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        this._resourceData = e.detail.model;
    }

    #onInjectionModeChange(e: Event) {
        this._injectionMode = (e.target as HTMLSelectElement).value as UaiContextResourceInjectionMode;
    }

    override render() {
        const isEditing = !!this.data?.resource;
        const headline = isEditing
            ? `Edit ${this.data?.resourceType?.name ?? 'Resource'}`
            : `Add ${this.data?.resourceType?.name ?? 'Resource'}`;

        return html`
            <umb-body-layout headline=${headline}>
                <div >
                    <uui-box headline="General">
                        <umb-property-layout label="Name" description="The name of the resource as referenced by AI models.">
                            <div slot="editor">
                                <uui-input
                                    id="name"
                                    .value=${this._resourceName}
                                    @input=${this.#onNameChange}
                                    placeholder="Enter resource name"
                                    required></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout label="Description" description="An optional description for the resource.">
                            <div slot="editor">
                                <uui-input
                                    id="description"
                                    .value=${this._resourceDescription}
                                    @input=${this.#onDescriptionChange}
                                    placeholder="Enter description"></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout label="Injection Mode" description="How this resource should be injected into AI requests">
                            <div slot="editor">
                                <uui-select
                                        id="injectionMode"
                                        .value=${this._injectionMode}
                                        .options=${[
                                            { value: 'Always', name: ' Always - Include in every request', selected: this._injectionMode === 'Always' },
                                            { value: 'OnDemand', name: 'On-Demand - Available via tool for LLM to retrieve', selected: this._injectionMode === 'OnDemand' },
                                        ]}
                                        @change=${this.#onInjectionModeChange}>
                                </uui-select>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                    <uui-box headline="Data">
                        <uai-model-editor
                            .schema=${this.data?.resourceType?.dataSchema}
                            .model=${this._resourceData}
                            @change=${this.#onDataChange}>
                        </uai-model-editor>
                    </uui-box>
                </div>
                <div slot="actions">
                    <uui-button
                        label="Cancel"
                        @click=${this.#handleCancel}>
                        Cancel
                    </uui-button>
                    <uui-button
                        look="primary"
                        color="positive"
                        label=${isEditing ? 'Save' : 'Add'}
                        @click=${this.#handleSubmit}
                        ?disabled=${!this._resourceName.trim()}>
                        ${isEditing ? 'Save' : 'Add'}
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = [
        css`
            #main {
                padding: var(--uui-size-layout-1);
            }

            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }
            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            uui-form-layout-item {
                margin-bottom: var(--uui-size-space-5);
            }

            uui-input,
            uui-select {
                width: 100%;
            }
        `,
    ];
}

export { UaiResourceOptionsModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiResourceOptionsModalElement;
    }
}

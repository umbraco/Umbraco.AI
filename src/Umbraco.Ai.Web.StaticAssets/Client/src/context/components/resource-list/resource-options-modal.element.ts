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
    private _resourceContent = '';

    @state()
    private _injectionMode: UaiContextResourceInjectionMode = 'Always';

    override connectedCallback() {
        super.connectedCallback();

        // Populate from existing resource if editing
        if (this.data?.resource) {
            this._resourceName = this.data.resource.name;
            this._resourceDescription = this.data.resource.description ?? '';
            this._resourceContent = this.data.resource.data;
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
            data: this._resourceContent,
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

    #onContentChange(e: Event) {
        this._resourceContent = (e.target as HTMLTextAreaElement).value;
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
                <div id="main">
                    <uui-box>
                        <uui-form>
                            <uui-form-layout-item>
                                <uui-label for="name" slot="label" required>Name</uui-label>
                                <uui-input
                                    id="name"
                                    .value=${this._resourceName}
                                    @input=${this.#onNameChange}
                                    placeholder="Enter resource name"
                                    required></uui-input>
                            </uui-form-layout-item>

                            <uui-form-layout-item>
                                <uui-label for="description" slot="label">Description</uui-label>
                                <span slot="description">Optional description for this resource</span>
                                <uui-input
                                    id="description"
                                    .value=${this._resourceDescription}
                                    @input=${this.#onDescriptionChange}
                                    placeholder="Enter description"></uui-input>
                            </uui-form-layout-item>

                            <uui-form-layout-item>
                                <uui-label for="injectionMode" slot="label">Injection Mode</uui-label>
                                <span slot="description">How this resource should be injected into AI requests</span>
                                <uui-select
                                    id="injectionMode"
                                    .value=${this._injectionMode}
                                    @change=${this.#onInjectionModeChange}>
                                    <uui-select-option value="Always" ?selected=${this._injectionMode === 'Always'}>
                                        Always - Include in every request
                                    </uui-select-option>
                                    <uui-select-option value="OnDemand" ?selected=${this._injectionMode === 'OnDemand'}>
                                        On-Demand - Available via tool for LLM to retrieve
                                    </uui-select-option>
                                </uui-select>
                            </uui-form-layout-item>

                            <uui-form-layout-item>
                                <uui-label for="content" slot="label">Content</uui-label>
                                <span slot="description">The content of this resource</span>
                                <uui-textarea
                                    id="content"
                                    .value=${this._resourceContent}
                                    @input=${this.#onContentChange}
                                    placeholder="Enter resource content..."
                                    rows="10"></uui-textarea>
                            </uui-form-layout-item>
                        </uui-form>
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

            uui-form-layout-item {
                margin-bottom: var(--uui-size-space-5);
            }

            uui-input,
            uui-select,
            uui-textarea {
                width: 100%;
            }

            uui-textarea {
                min-height: 200px;
                font-family: var(--uui-font-family-monospace);
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

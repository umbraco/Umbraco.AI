import {
    css,
    customElement,
    html,
    repeat,
    state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import type { UaiContextResourceTypeItemModel } from '../../../context-resource-type/types.js';
import type { UaiContextResourceTypePickerModalData, UaiContextResourceTypePickerModalValue } from './context-resource-type-picker-modal.token.js';

const elementName = 'uai-context-resource-type-picker-modal';

@customElement(elementName)
export class UaiContextResourceTypePickerModalElement extends UmbModalBaseElement<
    UaiContextResourceTypePickerModalData,
    UaiContextResourceTypePickerModalValue
> {
    @state()
    private _contextResourceTypes: UaiContextResourceTypeItemModel[] = [];

    override connectedCallback() {
        super.connectedCallback();
        this._contextResourceTypes = this.data?.contextResourceTypes ?? [];
    }

    #handleSelect(contextResourceType: UaiContextResourceTypeItemModel) {
        this.modalContext?.setValue({ contextResourceType });
        this.modalContext?.submit();
    }

    override render() {
        return html`
            <umb-body-layout headline=${this.data?.headline ?? 'Select Resource Type'}>
                <div id="main">
                    <uui-box>
                        <uui-ref-list>
                            ${repeat(
                                this._contextResourceTypes,
                                (type) => type.id,
                                (type) => this.#renderContextResourceType(type),
                            )}
                        </uui-ref-list>
                    </uui-box>
                </div>
                <uui-button
                    slot="actions"
                    label=${this.localize.term('general_close')}
                    @click=${this._rejectModal}>
                    ${this.localize.term('general_close')}
                </uui-button>
            </umb-body-layout>
        `;
    }

    #renderContextResourceType(type: UaiContextResourceTypeItemModel) {
        return html`
            <uui-ref-node
                name=${type.name}
                detail=${type.description ?? ''}
                @open=${() => this.#handleSelect(type)}>
                <umb-icon slot="icon" name=${type.icon ?? 'icon-document'}></umb-icon>
            </uui-ref-node>
        `;
    }

    static override styles = [
        UmbTextStyles,
        css`
            :host {
                position: relative;
            }

            #main {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-5);
            }

            uui-ref-node {
                padding-top: calc(var(--uui-size-2, 6px) + 5px);
                padding-bottom: calc(var(--uui-size-2, 6px) + 5px);
            }

            uui-ref-node::before {
                border-top: 1px solid var(--uui-color-divider-standalone);
            }
        `,
    ];
}

export { UaiContextResourceTypePickerModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiContextResourceTypePickerModalElement;
    }
}

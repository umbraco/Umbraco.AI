import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { umbExtensionsRegistry } from "@umbraco-cms/backoffice/extension-registry";
import type {
    UaiMockEntityEditorModalData,
    UaiMockEntityEditorModalValue,
} from "./mock-entity-editor-modal.token.js";
import {
    UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
    type ManifestTestMockEntityEditor,
} from "./mock-entity-editor-extension-type.js";

const elementName = "uai-mock-entity-editor-modal";

/**
 * Generic modal that hosts a registered mock entity editor element.
 * Looks up the uaiTestMockEntityEditor extension for the given entity type
 * and dynamically creates and manages the editor element.
 */
@customElement(elementName)
export class UaiMockEntityEditorModalElement extends UmbModalBaseElement<
    UaiMockEntityEditorModalData,
    UaiMockEntityEditorModalValue
> {
    @state()
    private _currentValue?: string;

    @state()
    private _loading = true;

    #editorElement?: HTMLElement & { entityType?: string; subType?: string; subTypeUnique?: string; value?: string };

    override async firstUpdated() {
        await this.#loadEditor();
    }

    async #loadEditor() {
        if (!this.data?.entityType) {
            this._loading = false;
            return;
        }

        // Find registered editor for this entity type
        const extensions = umbExtensionsRegistry.getByType(
            UAI_TEST_MOCK_ENTITY_EDITOR_EXTENSION_TYPE,
        ) as ManifestTestMockEntityEditor[];
        const manifest = extensions.find((ext) =>
            ext.forEntityTypes.includes(this.data!.entityType),
        );

        if (!manifest) {
            this._loading = false;
            return;
        }

        try {
            const module = await manifest.element();
            const ElementCtor =
                module.default ??
                module.element ??
                Object.values(module).find((v: unknown) => typeof v === "function");
            if (!ElementCtor) {
                this._loading = false;
                return;
            }

            const el = new (ElementCtor as new () => HTMLElement & {
                entityType?: string;
                subType?: string;
                subTypeUnique?: string;
                value?: string;
            })();

            el.entityType = this.data.entityType;
            el.subType = this.data.subTypeAlias;
            el.subTypeUnique = this.data.subTypeUnique;
            if (this.data.existingValue) {
                el.value = this.data.existingValue;
            }
            this._currentValue = this.data.existingValue;

            el.addEventListener("change", () => {
                this._currentValue = el.value;
            });

            this.#editorElement = el;
        } catch (error) {
            console.error("Failed to load mock entity editor:", error);
        }

        this._loading = false;
    }

    override updated() {
        // Manually manage the dynamic editor element in the DOM
        const host = this.shadowRoot?.querySelector("#editor-host");
        if (host && this.#editorElement && !host.contains(this.#editorElement)) {
            host.replaceChildren(this.#editorElement);
        }
    }

    #onSubmit() {
        if (!this._currentValue) return;
        this.value = { mockEntityJson: this._currentValue };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    override render() {
        const subTypeName = this.data?.subTypeName ?? this.data?.subTypeAlias ?? "";

        if (this._loading) {
            return html`
                <umb-body-layout headline="Mock ${subTypeName} Entity">
                    <uui-loader></uui-loader>
                </umb-body-layout>
            `;
        }

        return html`
            <umb-body-layout headline="Mock ${subTypeName} Entity">
                <div id="editor-host"></div>
                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}>Cancel</uui-button>
                    <uui-button
                        look="primary"
                        color="positive"
                        label="Submit"
                        ?disabled=${!this._currentValue}
                        @click=${this.#onSubmit}
                    >
                        Submit
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = css`
        #editor-host {
            padding: var(--uui-size-space-4) 0;
        }
    `;
}

export default UaiMockEntityEditorModalElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiMockEntityEditorModalElement;
    }
}

import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";

const elementName = "uai-json-mock-entity-editor";

const MOCK_ENTITY_TEMPLATE = `{
  "entityType": "document",
  "unique": "mock",
  "name": "Sample Entity",
  "data": {
    "contentType": "blogPost",
    "properties": [
      {
        "alias": "title",
        "label": "Title",
        "editorAlias": "Umbraco.TextBox",
        "value": "Hello World"
      }
    ]
  }
}`;

/**
 * Raw JSON editor for mock entity data.
 * Fallback editor used when no custom mock entity editor is registered for the entity type.
 */
@customElement(elementName)
export class UaiJsonMockEntityEditorElement extends UmbLitElement {
    @property({ type: String })
    entityType = "document";

    @property({ type: String })
    subType?: string;

    @property({ type: String })
    value?: string;

    @property({ type: Boolean })
    readonly = false;

    @state()
    private _jsonError?: string;

    #onChange(e: Event) {
        const textarea = e.target as HTMLTextAreaElement;
        const raw = textarea.value;

        // Validate JSON
        try {
            JSON.parse(raw);
            this._jsonError = undefined;
        } catch (err) {
            this._jsonError = (err as Error).message;
        }

        this.value = raw;
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onInsertTemplate() {
        // Pre-fill with entity type and sub-type
        const template = JSON.parse(MOCK_ENTITY_TEMPLATE);
        template.entityType = this.entityType;
        if (this.subType) {
            template.data.contentType = this.subType;
        }
        this.value = JSON.stringify(template, null, 2);
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        return html`
            <div class="json-editor">
                <textarea
                    .value=${this.value ?? ""}
                    rows="12"
                    ?disabled=${this.readonly}
                    placeholder=${MOCK_ENTITY_TEMPLATE}
                    @input=${this.#onChange}></textarea>
                ${this._jsonError
                    ? html`<div class="json-error">${this._jsonError}</div>`
                    : html``}
                ${!this.value && !this.readonly
                    ? html`<uui-button
                        look="secondary"
                        label="Insert template"
                        @click=${this.#onInsertTemplate}>
                    </uui-button>`
                    : html``}
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }
        .json-editor {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }
        textarea {
            width: 100%;
            font-family: monospace;
            font-size: 12px;
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            resize: vertical;
            box-sizing: border-box;
        }
        textarea:disabled {
            opacity: 0.7;
        }
        .json-error {
            color: var(--uui-color-danger);
            font-size: 12px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiJsonMockEntityEditorElement;
    }
}

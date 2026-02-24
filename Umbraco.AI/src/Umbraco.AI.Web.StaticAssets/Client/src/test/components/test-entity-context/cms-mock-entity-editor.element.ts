import { css, customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbDocumentTypeDetailRepository } from "@umbraco-cms/backoffice/document-type";
import { UmbMediaTypeDetailRepository } from "@umbraco-cms/backoffice/media-type";
import { UmbMemberTypeDetailRepository } from "@umbraco-cms/backoffice/member-type";

const elementName = "uai-cms-mock-entity-editor";

/**
 * Internal type for a content type property with data type reference.
 * The actual repository data has these fields but we narrow to what we need.
 */
interface ContentTypeProperty {
    alias: string;
    name: string;
    description?: string;
    dataType: { unique: string };
    sortOrder?: number;
    validation?: { mandatory: boolean };
}

interface ContentTypeDetail {
    properties: ContentTypeProperty[];
    compositions: Array<{ contentType: { unique: string } }>;
}

interface ResolvedProperty {
    alias: string;
    name: string;
    description?: string;
}

/**
 * CMS Mock Entity Editor.
 * Registered for document/media/member entity types.
 * Fetches content type properties and renders form fields.
 *
 * @fires change - Fires UmbChangeEvent when value changes.
 */
@customElement(elementName)
export class UaiCmsMockEntityEditorElement extends UmbLitElement {
    @property({ type: String })
    entityType = "document";

    @property({ type: String })
    subType?: string;

    @property({ type: String })
    subTypeUnique?: string;

    @property({ type: String })
    value?: string;

    @state()
    private _loading = false;

    @state()
    private _entityName = "";

    @state()
    private _entityUnique?: string;

    @state()
    private _resolvedProperties: ResolvedProperty[] = [];

    @state()
    private _propertyValues: Record<string, unknown> = {};

    // Lazy repository instances
    #documentTypeRepo?: UmbDocumentTypeDetailRepository;
    #mediaTypeRepo?: UmbMediaTypeDetailRepository;
    #memberTypeRepo?: UmbMemberTypeDetailRepository;

    #initialized = false;

    override async firstUpdated() {
        this.#deserialize();
        await this.#loadContentType();
        this.#initialized = true;
    }

    #deserialize() {
        if (!this.value) return;
        try {
            const entity = JSON.parse(this.value);
            this._entityName = entity.name ?? "";
            this._entityUnique = entity.unique;
            if (entity.data?.properties) {
                const values: Record<string, unknown> = {};
                for (const p of entity.data.properties) {
                    values[p.alias] = p.value;
                }
                this._propertyValues = values;
            }
        } catch {
            // Invalid JSON — leave defaults
        }
    }

    async #loadContentType() {
        if (!this.subTypeUnique) return;

        this._loading = true;

        try {
            const fetcher = this.#getTypeFetcher();
            if (!fetcher) {
                this._loading = false;
                return;
            }

            const contentType = await fetcher(this.subTypeUnique);
            if (!contentType) {
                this._loading = false;
                return;
            }

            // Collect all properties (direct + compositions)
            const allProperties: ContentTypeProperty[] = [...(contentType.properties ?? [])];

            if (contentType.compositions?.length) {
                const compResults = await Promise.all(
                    contentType.compositions.map(async (comp) => {
                        const compUnique = comp.contentType?.unique;
                        if (!compUnique) return [];
                        const compType = await fetcher(compUnique);
                        return compType?.properties ?? [];
                    }),
                );
                for (const props of compResults) {
                    allProperties.push(...props);
                }
            }

            // Sort by sortOrder then name
            allProperties.sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name));

            this._resolvedProperties = allProperties.map((prop) => ({
                alias: prop.alias,
                name: prop.name,
                description: prop.description,
            }));
        } catch (error) {
            console.error("Failed to load content type:", error);
        }

        this._loading = false;
    }

    #getTypeFetcher() {
        switch (this.entityType) {
            case "document":
                return async (unique: string): Promise<ContentTypeDetail | undefined> => {
                    this.#documentTypeRepo ??= new UmbDocumentTypeDetailRepository(this);
                    const { data } = await this.#documentTypeRepo.requestByUnique(unique);
                    return data as ContentTypeDetail | undefined;
                };
            case "media":
                return async (unique: string): Promise<ContentTypeDetail | undefined> => {
                    this.#mediaTypeRepo ??= new UmbMediaTypeDetailRepository(this);
                    const { data } = await this.#mediaTypeRepo.requestByUnique(unique);
                    return data as ContentTypeDetail | undefined;
                };
            case "member":
                return async (unique: string): Promise<ContentTypeDetail | undefined> => {
                    this.#memberTypeRepo ??= new UmbMemberTypeDetailRepository(this);
                    const { data } = await this.#memberTypeRepo.requestByUnique(unique);
                    return data as ContentTypeDetail | undefined;
                };
        }
        return undefined;
    }

    #serialize() {
        const entity = {
            entityType: this.entityType,
            unique: this._entityUnique ?? `mock-${crypto.randomUUID()}`,
            name: this._entityName,
            data: {
                contentType: this.subType,
                properties: this._resolvedProperties.map((prop) => ({
                    alias: prop.alias,
                    label: prop.name,
                    value: this._propertyValues[prop.alias] ?? null,
                })),
            },
        };
        return JSON.stringify(entity, null, 2);
    }

    #emitValue() {
        this.value = this.#serialize();
        this.dispatchEvent(new UmbChangeEvent());
    }

    #onNameChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._entityName = input.value;
        // Preserve unique once set
        if (!this._entityUnique) {
            this._entityUnique = `mock-${crypto.randomUUID()}`;
        }
        this.#emitValue();
    }

    #onPropertyChange(alias: string, e: Event) {
        const input = e.target as HTMLInputElement | HTMLTextAreaElement;
        this._propertyValues = { ...this._propertyValues, [alias]: input.value };
        this.#emitValue();
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._resolvedProperties.length === 0 && this.#initialized) {
            return html`<uui-label><em>No properties found for this content type.</em></uui-label>`;
        }

        return html`
            <div class="cms-editor">
                <umb-property-layout label="Name" description="Entity display name">
                    <uui-input
                        slot="editor"
                        .value=${this._entityName}
                        @input=${this.#onNameChange}
                        placeholder="Enter entity name"
                    ></uui-input>
                </umb-property-layout>
                ${this._resolvedProperties.map((prop) => this.#renderProperty(prop))}
            </div>
        `;
    }

    #renderProperty(prop: ResolvedProperty) {
        const currentValue = this._propertyValues[prop.alias];
        const isLongValue = typeof currentValue === "string" && currentValue.length > 100;

        return html`
            <umb-property-layout
                label=${prop.name}
                .description=${prop.description ?? prop.alias}
            >
                ${isLongValue || this.#looksLikeRichText(prop.alias)
                    ? html`
                        <textarea
                            slot="editor"
                            .value=${(currentValue as string) ?? ""}
                            rows="4"
                            @input=${(e: Event) => this.#onPropertyChange(prop.alias, e)}
                            placeholder=${prop.alias}
                        ></textarea>`
                    : html`
                        <uui-input
                            slot="editor"
                            .value=${currentValue != null ? String(currentValue) : ""}
                            @input=${(e: Event) => this.#onPropertyChange(prop.alias, e)}
                            placeholder=${prop.alias}
                        ></uui-input>`}
            </umb-property-layout>
        `;
    }

    #looksLikeRichText(alias: string): boolean {
        const lower = alias.toLowerCase();
        return lower.includes("body") || lower.includes("content") || lower.includes("description") || lower.includes("text");
    }

    static override styles = css`
        :host {
            display: block;
        }
        .cms-editor {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }
        uui-input {
            width: 100%;
        }
        textarea {
            width: 100%;
            font-family: inherit;
            font-size: inherit;
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            resize: vertical;
            box-sizing: border-box;
        }
    `;
}

export { UaiCmsMockEntityEditorElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiCmsMockEntityEditorElement;
    }
}

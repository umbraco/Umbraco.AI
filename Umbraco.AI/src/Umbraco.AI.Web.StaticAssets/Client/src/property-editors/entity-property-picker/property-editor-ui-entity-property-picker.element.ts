import { customElement, html, property, state } from "@umbraco-cms/backoffice/external/lit";
import { debounceTime, distinctUntilChanged, map } from "@umbraco-cms/backoffice/external/rxjs";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UMB_PROPERTY_DATASET_CONTEXT } from "@umbraco-cms/backoffice/property";
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from "@umbraco-cms/backoffice/property-editor";
import { UmbDocumentDetailRepository } from "@umbraco-cms/backoffice/document";
import { UmbMediaDetailRepository } from "@umbraco-cms/backoffice/media";
import { UmbMemberDetailRepository } from "@umbraco-cms/backoffice/member";
import { UmbDocumentTypeDetailRepository } from "@umbraco-cms/backoffice/document-type";
import { UmbMediaTypeDetailRepository } from "@umbraco-cms/backoffice/media-type";
import { UmbMemberTypeDetailRepository } from "@umbraco-cms/backoffice/member-type";

interface PropertyOption {
    name: string;
    value: string;
    selected?: boolean;
}

const elementName = "uai-property-editor-ui-entity-property-picker";

@customElement(elementName)
export class UaiPropertyEditorUIEntityPropertyPickerElement
    extends UmbFormControlMixin<string | undefined, typeof UmbLitElement>(UmbLitElement, undefined)
    implements UmbPropertyEditorUiElement
{
    @property({ type: Boolean })
    public readonly = false;

    @state()
    private _entityType?: string;

    @state()
    private _entityId?: string;

    @state()
    private _loading = false;

    @state()
    private _propertyOptions: Array<PropertyOption> = [];

    private _entityTypeField = "entityType";
    private _entityIdField = "entityId";
    private _loadRequestId = 0;

    // Lazy repository instances
    #documentDetailRepo?: UmbDocumentDetailRepository;
    #mediaDetailRepo?: UmbMediaDetailRepository;
    #memberDetailRepo?: UmbMemberDetailRepository;
    #documentTypeDetailRepo?: UmbDocumentTypeDetailRepository;
    #mediaTypeDetailRepo?: UmbMediaTypeDetailRepository;
    #memberTypeDetailRepo?: UmbMemberTypeDetailRepository;

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        if (!config) return;
        this._entityTypeField = config.getValueByAlias<string>("entityTypeField") ?? "entityType";
        this._entityIdField = config.getValueByAlias<string>("entityIdField") ?? "entityId";
    }

    constructor() {
        super();

        this.consumeContext(UMB_PROPERTY_DATASET_CONTEXT, async (context) => {
            if (!context) return;

            // Both observers use debounceTime to absorb the brief old-value re-emission
            // during save-rebind, and distinctUntilChanged to skip no-op updates.

            const entityType$ = await context.propertyValueByAlias<string>(this._entityTypeField);
            if (entityType$) {
                this.observe(
                    entityType$.pipe(
                        map((v) => (Array.isArray(v) ? v[0] : v)),
                        debounceTime(50),
                        distinctUntilChanged(),
                    ),
                    (entityType) => {
                        // Clear only when switching between two different defined types
                        if (this._entityType && entityType && this._entityType !== entityType) {
                            this.value = undefined;
                            this._propertyOptions = [];
                            this.dispatchEvent(new UmbChangeEvent());
                        }

                        this._entityType = entityType;
                        this.#loadProperties();
                    },
                    "_observeEntityType",
                );
            }

            const entityId$ = await context.propertyValueByAlias<string>(this._entityIdField);
            if (entityId$) {
                this.observe(
                    entityId$.pipe(
                        map((v) => (Array.isArray(v) ? v[0] : v)),
                        debounceTime(50),
                        distinctUntilChanged(),
                    ),
                    (entityId) => {
                        // Clear only when switching between two different defined IDs
                        if (this._entityId && entityId && this._entityId !== entityId) {
                            this.value = undefined;
                            this.dispatchEvent(new UmbChangeEvent());
                        }

                        this._entityId = entityId;
                        this.#loadProperties();
                    },
                    "_observeEntityId",
                );
            }
        });
    }

    async #loadProperties() {
        const entityType = this._entityType;
        const entityId = this._entityId;

        if (!entityType || !entityId) {
            this._propertyOptions = [];
            this._loading = false;
            return;
        }

        const requestId = ++this._loadRequestId;
        this._loading = true;

        try {
            // Step 1: Get content type unique from entity detail
            let contentTypeUnique: string | undefined;

            switch (entityType) {
                case "document": {
                    this.#documentDetailRepo ??= new UmbDocumentDetailRepository(this);
                    const { data } = await this.#documentDetailRepo.requestByUnique(entityId);
                    contentTypeUnique = data?.documentType?.unique;
                    break;
                }
                case "media": {
                    this.#mediaDetailRepo ??= new UmbMediaDetailRepository(this);
                    const { data } = await this.#mediaDetailRepo.requestByUnique(entityId);
                    contentTypeUnique = data?.mediaType?.unique;
                    break;
                }
                case "member": {
                    this.#memberDetailRepo ??= new UmbMemberDetailRepository(this);
                    const { data } = await this.#memberDetailRepo.requestByUnique(entityId);
                    contentTypeUnique = data?.memberType?.unique;
                    break;
                }
            }

            if (requestId !== this._loadRequestId) return;

            if (!contentTypeUnique) {
                this._propertyOptions = [];
                this._loading = false;
                return;
            }

            // Step 2: Get all properties (direct + compositions)
            const allProperties = await this.#collectAllProperties(entityType, contentTypeUnique);

            if (requestId !== this._loadRequestId) return;

            allProperties.sort((a, b) => a.name.localeCompare(b.name));
            this._propertyOptions = allProperties.map((p) => ({
                name: `${p.name} (${p.alias})`,
                value: p.alias,
                selected: p.alias === this.value,
            }));
        } catch {
            if (requestId !== this._loadRequestId) return;
            this._propertyOptions = [];
        } finally {
            if (requestId === this._loadRequestId) {
                this._loading = false;
            }
        }
    }

    /**
     * Fetches a content type and collects its direct properties plus
     * properties inherited from all compositions.
     */
    async #collectAllProperties(
        entityType: string,
        contentTypeUnique: string,
    ): Promise<Array<{ alias: string; name: string }>> {
        const fetchType = this.#getTypeFetcher(entityType);
        if (!fetchType) return [];

        const contentType = await fetchType(contentTypeUnique);
        if (!contentType) return [];

        const properties: Array<{ alias: string; name: string }> = [
            ...(contentType.properties ?? []),
        ];

        // Load composition properties in parallel
        const compositions = contentType.compositions ?? [];
        if (compositions.length > 0) {
            const compositionResults = await Promise.all(
                compositions.map(async (comp) => {
                    const compUnique = comp.contentType?.unique;
                    if (!compUnique) return [];
                    const compType = await fetchType(compUnique);
                    return compType?.properties ?? [];
                }),
            );
            for (const compProps of compositionResults) {
                properties.push(...compProps);
            }
        }

        return properties;
    }

    /**
     * Returns a function that fetches a content type by unique and returns
     * its properties and compositions. Lazily initializes the type repository.
     */
    #getTypeFetcher(entityType: string) {
        type ContentTypeResult = {
            properties: Array<{ alias: string; name: string }>;
            compositions: Array<{ contentType: { unique: string } }>;
        };

        switch (entityType) {
            case "document":
                return async (unique: string): Promise<ContentTypeResult | undefined> => {
                    this.#documentTypeDetailRepo ??= new UmbDocumentTypeDetailRepository(this);
                    const { data } = await this.#documentTypeDetailRepo.requestByUnique(unique);
                    return data as ContentTypeResult | undefined;
                };
            case "media":
                return async (unique: string): Promise<ContentTypeResult | undefined> => {
                    this.#mediaTypeDetailRepo ??= new UmbMediaTypeDetailRepository(this);
                    const { data } = await this.#mediaTypeDetailRepo.requestByUnique(unique);
                    return data as ContentTypeResult | undefined;
                };
            case "member":
                return async (unique: string): Promise<ContentTypeResult | undefined> => {
                    this.#memberTypeDetailRepo ??= new UmbMemberTypeDetailRepository(this);
                    const { data } = await this.#memberTypeDetailRepo.requestByUnique(unique);
                    return data as ContentTypeResult | undefined;
                };
        }
        return undefined;
    }

    #onSelectChange(e: Event) {
        const target = e.target as HTMLElement & { value: string };
        this.value = target.value || undefined;
        this.dispatchEvent(new UmbChangeEvent());
    }

    override render() {
        if (!this._entityType || !this._entityId) {
            return html`<uui-label><em>Select an entity first</em></uui-label>`;
        }

        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._propertyOptions.length === 0) {
            return html`<uui-label><em>No properties found</em></uui-label>`;
        }

        return html`
            <uui-select
                label="Property"
                .options=${this._propertyOptions}
                .value=${this.value ?? ""}
                ?disabled=${this.readonly}
                @change=${this.#onSelectChange}>
            </uui-select>
        `;
    }
}

export { UaiPropertyEditorUIEntityPropertyPickerElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiPropertyEditorUIEntityPropertyPickerElement;
    }
}

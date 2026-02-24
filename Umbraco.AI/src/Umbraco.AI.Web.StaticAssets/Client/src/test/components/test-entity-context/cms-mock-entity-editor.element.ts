import { css, customElement, html, nothing, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbDocumentTypeDetailRepository } from "@umbraco-cms/backoffice/document-type";
import { UmbMediaTypeDetailRepository } from "@umbraco-cms/backoffice/media-type";
import { UmbMemberTypeDetailRepository } from "@umbraco-cms/backoffice/member-type";
import type {
    UmbPropertyTypeModel,
    UmbPropertyTypeContainerModel,
    UmbContentTypeModel,
} from "@umbraco-cms/backoffice/content-type";
import type { UmbPropertyValueData, UmbPropertyDatasetElement } from "@umbraco-cms/backoffice/property";

const elementName = "uai-cms-mock-entity-editor";

/** A tab with its child groups and any root-level properties. */
interface TabViewModel {
    id: string;
    name: string;
    sortOrder: number;
    /** Groups within this tab. */
    groups: GroupViewModel[];
    /** Properties directly on the tab (not in a group). */
    rootProperties: UmbPropertyTypeModel[];
}

/** A group (rendered as a box) with its properties. */
interface GroupViewModel {
    id: string;
    name: string;
    sortOrder: number;
    properties: UmbPropertyTypeModel[];
}

/**
 * CMS Mock Entity Editor.
 * Registered for document/media/member entity types.
 * Fetches content type structure and renders properties in tabs and groups
 * using `umb-property-type-based-property` within `umb-property-dataset`.
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
    private _tabs: TabViewModel[] = [];

    /** Properties that have no container (no tab, no group). */
    @state()
    private _rootProperties: UmbPropertyTypeModel[] = [];

    /** All resolved property types from the content type + compositions. */
    @state()
    private _allProperties: UmbPropertyTypeModel[] = [];

    @state()
    private _propertyValues: UmbPropertyValueData[] = [];

    @state()
    private _activeTabId?: string;

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
                this._propertyValues = entity.data.properties.map((p: { alias: string; value: unknown }) => ({
                    alias: p.alias,
                    value: p.value,
                }));
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

            // Collect all properties and containers (direct + compositions)
            const allProperties: UmbPropertyTypeModel[] = [...(contentType.properties ?? [])];
            const allContainers: UmbPropertyTypeContainerModel[] = [...(contentType.containers ?? [])];

            if (contentType.compositions?.length) {
                const compResults = await Promise.all(
                    contentType.compositions.map(async (comp) => {
                        const compUnique = comp.contentType?.unique;
                        if (!compUnique) return { properties: [] as UmbPropertyTypeModel[], containers: [] as UmbPropertyTypeContainerModel[] };
                        const compType = await fetcher(compUnique);
                        return {
                            properties: compType?.properties ?? [],
                            containers: compType?.containers ?? [],
                        };
                    }),
                );
                for (const result of compResults) {
                    allProperties.push(...result.properties);
                    allContainers.push(...result.containers);
                }
            }

            // Sort properties by sortOrder then name
            allProperties.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));

            this._allProperties = allProperties;
            this.#buildViewModel(allProperties, allContainers);

            // Initialize property values for any properties not yet in the values array
            const existingAliases = new Set(this._propertyValues.map((v) => v.alias));
            const newValues = allProperties
                .filter((p) => !existingAliases.has(p.alias))
                .map((p) => ({ alias: p.alias, value: undefined }));
            if (newValues.length > 0) {
                this._propertyValues = [...this._propertyValues, ...newValues];
            }
        } catch (error) {
            console.error("Failed to load content type:", error);
        }

        this._loading = false;
    }

    #buildViewModel(properties: UmbPropertyTypeModel[], containers: UmbPropertyTypeContainerModel[]) {
        // Index containers by id
        const containerMap = new Map<string, UmbPropertyTypeContainerModel>();
        for (const c of containers) {
            containerMap.set(c.id, c);
        }

        // Separate tabs and groups
        const tabs = containers.filter((c) => c.type === "Tab" && !c.parent);
        const groups = containers.filter((c) => c.type === "Group");

        // Build a map: containerId → properties
        const propsByContainer = new Map<string, UmbPropertyTypeModel[]>();
        const rootProps: UmbPropertyTypeModel[] = [];

        for (const prop of properties) {
            if (!prop.container?.id) {
                rootProps.push(prop);
            } else {
                const list = propsByContainer.get(prop.container.id) ?? [];
                list.push(prop);
                propsByContainer.set(prop.container.id, list);
            }
        }

        // Build group view models
        const buildGroup = (container: UmbPropertyTypeContainerModel): GroupViewModel => ({
            id: container.id,
            name: container.name,
            sortOrder: container.sortOrder,
            properties: propsByContainer.get(container.id) ?? [],
        });

        // Build tab view models
        const tabViewModels: TabViewModel[] = tabs
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map((tab) => {
                const childGroups = groups
                    .filter((g) => g.parent?.id === tab.id)
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map(buildGroup);

                return {
                    id: tab.id,
                    name: tab.name,
                    sortOrder: tab.sortOrder,
                    groups: childGroups,
                    rootProperties: propsByContainer.get(tab.id) ?? [],
                };
            });

        // Groups without a parent tab (standalone groups)
        const standaloneGroups = groups
            .filter((g) => !g.parent)
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map(buildGroup);

        // If there are standalone groups, create a synthetic tab for them
        if (standaloneGroups.length > 0) {
            tabViewModels.unshift({
                id: "__standalone__",
                name: "Content",
                sortOrder: -1,
                groups: standaloneGroups,
                rootProperties: rootProps,
            });
            this._rootProperties = [];
        } else {
            this._rootProperties = rootProps;
        }

        this._tabs = tabViewModels;

        // Set active tab to first
        if (tabViewModels.length > 0 && !this._activeTabId) {
            this._activeTabId = tabViewModels[0].id;
        }
    }

    #getTypeFetcher() {
        switch (this.entityType) {
            case "document":
                return async (unique: string): Promise<UmbContentTypeModel | undefined> => {
                    this.#documentTypeRepo ??= new UmbDocumentTypeDetailRepository(this);
                    const { data } = await this.#documentTypeRepo.requestByUnique(unique);
                    return data as UmbContentTypeModel | undefined;
                };
            case "media":
                return async (unique: string): Promise<UmbContentTypeModel | undefined> => {
                    this.#mediaTypeRepo ??= new UmbMediaTypeDetailRepository(this);
                    const { data } = await this.#mediaTypeRepo.requestByUnique(unique);
                    return data as UmbContentTypeModel | undefined;
                };
            case "member":
                return async (unique: string): Promise<UmbContentTypeModel | undefined> => {
                    this.#memberTypeRepo ??= new UmbMemberTypeDetailRepository(this);
                    const { data } = await this.#memberTypeRepo.requestByUnique(unique);
                    return data as UmbContentTypeModel | undefined;
                };
        }
        return undefined;
    }

    #serialize(): string {
        const entity = {
            entityType: this.entityType,
            unique: this._entityUnique ?? `mock-${crypto.randomUUID()}`,
            name: this._entityName,
            data: {
                contentType: this.subType,
                properties: this._allProperties.map((prop) => {
                    const pv = this._propertyValues.find((v) => v.alias === prop.alias);
                    return {
                        alias: prop.alias,
                        label: prop.name,
                        value: pv?.value ?? null,
                    };
                }),
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
        if (!this._entityUnique) {
            this._entityUnique = `mock-${crypto.randomUUID()}`;
        }
        this.#emitValue();
    }

    #onPropertyDatasetChange(e: Event) {
        const dataset = e.target as UmbPropertyDatasetElement;
        this._propertyValues = [...dataset.value];
        this.#emitValue();
    }

    #onTabClick(tabId: string) {
        this._activeTabId = tabId;
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._allProperties.length === 0 && this.#initialized) {
            return html`<uui-label><em>No properties found for this content type.</em></uui-label>`;
        }

        return html`
            <div class="cms-editor">
                <umb-property-layout label="Name" description="Entity display name" style="padding-top: 0;">
                    <uui-input
                        slot="editor"
                        .value=${this._entityName}
                        @input=${this.#onNameChange}
                        placeholder="Enter entity name"
                    ></uui-input>
                </umb-property-layout>

                <umb-property-dataset .value=${this._propertyValues} @change=${this.#onPropertyDatasetChange}>
                    ${this._tabs.length > 1 ? this.#renderTabs() : nothing}
                    ${this._tabs.length === 1 ? this.#renderTabContent(this._tabs[0]) : nothing}
                    ${this._tabs.length === 0 ? this.#renderUngroupedProperties() : nothing}
                </umb-property-dataset>
            </div>
        `;
    }

    #renderTabs() {
        return html`
            <uui-tab-group>
                ${this._tabs.map(
                    (tab) => html`
                        <uui-tab
                            label=${tab.name}
                            ?active=${this._activeTabId === tab.id}
                            @click=${() => this.#onTabClick(tab.id)}
                        >${tab.name}</uui-tab>
                    `,
                )}
            </uui-tab-group>
            ${this._tabs.map(
                (tab) => html`
                    <div class="tab-content" ?hidden=${this._activeTabId !== tab.id}>
                        ${this.#renderTabContent(tab)}
                    </div>
                `,
            )}
        `;
    }

    #renderTabContent(tab: TabViewModel) {
        return html`
            ${tab.rootProperties.length > 0
                ? html`
                    <uui-box>
                        ${this.#renderProperties(tab.rootProperties)}
                    </uui-box>`
                : nothing}
            ${repeat(
                tab.groups,
                (group) => group.id,
                (group) => html`
                    <uui-box .headline=${group.name}>
                        ${this.#renderProperties(group.properties)}
                    </uui-box>
                `,
            )}
        `;
    }

    #renderUngroupedProperties() {
        if (this._rootProperties.length === 0) return nothing;
        return html`
            <uui-box>
                ${this.#renderProperties(this._rootProperties)}
            </uui-box>
        `;
    }

    #renderProperties(properties: UmbPropertyTypeModel[]) {
        return repeat(
            properties,
            (prop) => prop.alias,
            (prop) => html`
                <umb-property-type-based-property
                    .property=${prop}
                ></umb-property-type-based-property>
            `,
        );
    }

    static override styles = css`
        :host {
            display: block;
        }
        .cms-editor {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-4);
        }
        uui-input {
            width: 100%;
        }
        uui-tab-group {
            margin-bottom: var(--uui-size-space-4);
        }
        uui-box {
            --uui-box-default-padding: 0 var(--uui-size-space-5);
        }
        uui-box + uui-box {
            margin-top: var(--uui-size-layout-1);
        }
        .tab-content[hidden] {
            display: none;
        }
    `;
}

export { UaiCmsMockEntityEditorElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiCmsMockEntityEditorElement;
    }
}

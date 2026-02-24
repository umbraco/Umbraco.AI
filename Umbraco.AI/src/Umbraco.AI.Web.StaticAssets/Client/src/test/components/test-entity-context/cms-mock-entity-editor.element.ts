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

/**
 * A merged container that combines containers from the main type and compositions
 * that share the same name, type, and parent path.
 */
interface MergedContainer {
    key: string;
    ids: Set<string>;
    name: string;
    type: "Tab" | "Group";
    sortOrder: number;
    parentKey: string | null;
}

interface TabViewModel {
    key: string;
    name: string;
    sortOrder: number;
    groups: GroupViewModel[];
    rootProperties: UmbPropertyTypeModel[];
}

interface GroupViewModel {
    key: string;
    name: string;
    sortOrder: number;
    properties: UmbPropertyTypeModel[];
}

/**
 * CMS Mock Entity Editor.
 * Registered for document/media/member entity types.
 * Fetches content type structure and renders properties in tabs and groups
 * using `umb-property-type-based-property` within `umb-property-dataset`,
 * matching the document blueprint editor layout.
 *
 * Renders its own sticky header (name input + tabs) so the host modal
 * only needs to provide the outer chrome and footer actions.
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

    @state()
    private _rootProperties: UmbPropertyTypeModel[] = [];

    @state()
    private _allProperties: UmbPropertyTypeModel[] = [];

    @state()
    private _propertyValues: UmbPropertyValueData[] = [];

    @state()
    private _activeTabKey?: string;

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

            allProperties.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name));

            this._allProperties = allProperties;
            this.#buildViewModel(allProperties, allContainers);

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

    #mergeContainers(containers: UmbPropertyTypeContainerModel[]): MergedContainer[] {
        const byId = new Map<string, UmbPropertyTypeContainerModel>();
        for (const c of containers) {
            byId.set(c.id, c);
        }

        const resolvedKeyCache = new Map<string, string>();

        const resolveKey = (container: UmbPropertyTypeContainerModel): string => {
            const cached = resolvedKeyCache.get(container.id);
            if (cached) return cached;

            let parentKey: string | null = null;
            if (container.parent?.id) {
                const parent = byId.get(container.parent.id);
                if (parent) {
                    parentKey = resolveKey(parent);
                }
            }

            const key = `${container.type}::${parentKey ?? "root"}::${container.name}`;
            resolvedKeyCache.set(container.id, key);
            return key;
        };

        const mergedMap = new Map<string, MergedContainer>();

        for (const container of containers) {
            const key = resolveKey(container);
            const existing = mergedMap.get(key);

            if (existing) {
                existing.ids.add(container.id);
                if (container.sortOrder < existing.sortOrder) {
                    existing.sortOrder = container.sortOrder;
                }
            } else {
                let parentKey: string | null = null;
                if (container.parent?.id) {
                    const parent = byId.get(container.parent.id);
                    if (parent) {
                        parentKey = resolveKey(parent);
                    }
                }

                mergedMap.set(key, {
                    key,
                    ids: new Set([container.id]),
                    name: container.name,
                    type: container.type as "Tab" | "Group",
                    sortOrder: container.sortOrder,
                    parentKey,
                });
            }
        }

        return Array.from(mergedMap.values());
    }

    #buildViewModel(properties: UmbPropertyTypeModel[], containers: UmbPropertyTypeContainerModel[]) {
        const merged = this.#mergeContainers(containers);

        const idToMergedKey = new Map<string, string>();
        for (const mc of merged) {
            for (const id of mc.ids) {
                idToMergedKey.set(id, mc.key);
            }
        }

        const propsByMergedKey = new Map<string, UmbPropertyTypeModel[]>();
        const rootProps: UmbPropertyTypeModel[] = [];

        for (const prop of properties) {
            if (!prop.container?.id) {
                rootProps.push(prop);
            } else {
                const mergedKey = idToMergedKey.get(prop.container.id);
                if (mergedKey) {
                    const list = propsByMergedKey.get(mergedKey) ?? [];
                    list.push(prop);
                    propsByMergedKey.set(mergedKey, list);
                } else {
                    rootProps.push(prop);
                }
            }
        }

        const tabs = merged.filter((mc) => mc.type === "Tab" && !mc.parentKey);
        const groups = merged.filter((mc) => mc.type === "Group");

        const buildGroup = (mc: MergedContainer): GroupViewModel => ({
            key: mc.key,
            name: mc.name,
            sortOrder: mc.sortOrder,
            properties: propsByMergedKey.get(mc.key) ?? [],
        });

        const tabViewModels: TabViewModel[] = tabs
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map((tab) => {
                const childGroups = groups
                    .filter((g) => g.parentKey === tab.key)
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map(buildGroup);

                return {
                    key: tab.key,
                    name: tab.name,
                    sortOrder: tab.sortOrder,
                    groups: childGroups,
                    rootProperties: propsByMergedKey.get(tab.key) ?? [],
                };
            });

        const standaloneGroups = groups
            .filter((g) => !g.parentKey)
            .sort((a, b) => a.sortOrder - b.sortOrder)
            .map(buildGroup);

        if (standaloneGroups.length > 0) {
            tabViewModels.unshift({
                key: "__standalone__",
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

        if (tabViewModels.length > 0 && !this._activeTabKey) {
            this._activeTabKey = tabViewModels[0].key;
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

    #onTabClick(tabKey: string) {
        this._activeTabKey = tabKey;
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._allProperties.length === 0 && this.#initialized) {
            return html`<uui-label><em>No properties found for this content type.</em></uui-label>`;
        }

        return html`
            <div id="header">
                <uui-input
                    id="name-input"
                    .value=${this._entityName}
                    @input=${this.#onNameChange}
                    placeholder="Enter a name..."
                    label="Name"
                ></uui-input>
                ${this._tabs.length > 0
                    ? html`
                        <uui-tab-group>
                            ${this._tabs.map(
                                (tab) => html`
                                    <uui-tab
                                        label=${tab.name}
                                        ?active=${this._activeTabKey === tab.key}
                                        @click=${() => this.#onTabClick(tab.key)}
                                    >${tab.name}</uui-tab>
                                `,
                            )}
                        </uui-tab-group>`
                    : nothing}
            </div>

            <div id="body">
                <umb-property-dataset .value=${this._propertyValues} @change=${this.#onPropertyDatasetChange}>
                    ${this._tabs.length > 0
                        ? this._tabs.map(
                            (tab) => html`
                                <div class="tab-content" ?hidden=${this._activeTabKey !== tab.key}>
                                    ${this.#renderTabContent(tab)}
                                </div>
                            `,
                        )
                        : this.#renderUngroupedProperties()}
                </umb-property-dataset>
            </div>
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
                (group) => group.key,
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
            display: flex;
            flex-direction: column;
            height: 100%;
            overflow: hidden;
        }

        #header {
            position: sticky;
            top: 0;
            z-index: 1;
            background-color: var(--uui-color-surface);
            padding: var(--uui-size-layout-1) var(--uui-size-layout-1) 0;
            flex-shrink: 0;
        }

        #name-input {
            width: 100%;
            margin-bottom: var(--uui-size-space-4);
        }

        uui-tab-group {
            --uui-tab-divider: var(--uui-color-border);
            border-bottom: 1px solid var(--uui-color-border);
        }

        #body {
            flex: 1;
            overflow-y: auto;
            padding: var(--uui-size-layout-1);
            background-color: var(--uui-color-background);
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

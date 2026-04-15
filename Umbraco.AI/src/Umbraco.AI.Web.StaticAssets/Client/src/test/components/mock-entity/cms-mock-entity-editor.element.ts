import { css, html, customElement, nothing, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
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
import type { UmbRoute, UmbRouterSlotChangeEvent, UmbRouterSlotInitEvent } from "@umbraco-cms/backoffice/router";
import { encodeFolderName } from "@umbraco-cms/backoffice/router";
import type { GroupViewModel, MergedContainer, TabViewModel } from "./types.js";
import type { UaiCmsMockEntityEditorTabElement } from "./cms-mock-entity-editor-tab.element.js";

const elementName = "uai-cms-mock-entity-editor";

const ROOT_TAB_KEY = "__root__";
const ROOT_TAB_NAME = "Content";

/**
 * CMS Mock Entity Editor.
 * Registered for document/media/member entity types.
 *
 * Fetches content type structure and renders properties in tabs and groups
 * via a routed sub-view, mirroring the CMS block-workspace-view-edit layout.
 *
 * The outer editor hosts an `<umb-router-slot>` inside an `<umb-property-dataset>`:
 * the property dataset provides `UMB_PROPERTY_DATASET_CONTEXT` + an invariant
 * `UMB_VARIANT_CONTEXT`, and the router slot publishes `UMB_ROUTE_CONTEXT` so
 * nested block-based property editors (grid, list, rte, single) can register
 * their catalogue/workspace modals via `UmbModalRouteRegistrationController`.
 *
 * The modal-route for this editor is registered by the owning `uai-mock-entity`
 * element, so the tab sub-paths emitted by this router slot stay scoped under
 * that registered URL segment and are cleaned up when the modal closes.
 *
 * Renders its own sticky header (name input + tabs) so the host modal only
 * needs to provide the outer chrome and footer actions.
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
    private _allProperties: UmbPropertyTypeModel[] = [];

    @state()
    private _propertyValues: UmbPropertyValueData[] = [];

    @state()
    private _routes: UmbRoute[] = [];

    @state()
    private _routerPath?: string;

    @state()
    private _activePath = "";

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

        // Prepend a synthetic root tab for root-level properties and standalone groups
        // (the CMS blueprint calls this the "Generic" tab). We always promote to a tab
        // so everything renders through the router-slot path.
        const hasRootContent = rootProps.length > 0 || standaloneGroups.length > 0;
        if (hasRootContent) {
            tabViewModels.unshift({
                key: ROOT_TAB_KEY,
                name: ROOT_TAB_NAME,
                sortOrder: -1,
                groups: standaloneGroups,
                rootProperties: rootProps,
            });
        }

        this._tabs = tabViewModels;
        this.#createRoutes();
    }

    #createRoutes() {
        if (this._tabs.length === 0) {
            this._routes = [];
            return;
        }

        const routes: UmbRoute[] = this._tabs.map((tab) => ({
            path: this.#pathForTab(tab),
            component: () => import("./cms-mock-entity-editor-tab.element.js"),
            setup: (component) => {
                (component as UaiCmsMockEntityEditorTabElement).tab = tab;
            },
        }));

        // Default route: replaceState into the first tab's canonical path so the
        // URL reflects the active tab and the tab pill picks up the active style.
        // Mirrors document-workspace-editor.element.ts's default-route handling —
        // without this, matching path "" keeps the URL at the parent route's base
        // and absoluteActiveViewPath comes back without a tab segment, so
        // href-based active-state comparison fails for the landed-on tab.
        const firstTabPath = this.#pathForTab(this._tabs[0]);
        routes.push({
            path: "",
            pathMatch: "full",
            resolve: async () => {
                // Guard: when the modal is closing, UmbRouteContext._internal_removeModalPath
                // pushes the URL back to the outer workspace path. That triggers one last
                // pass through our inner router-slot on this empty-path route — if we
                // replaceState unconditionally, we'd yank the URL back into our modal
                // segment and undo the modal-manager's cleanup (the user sees the URL
                // clear and then reappear). Only redirect while the URL is still inside
                // our modal's registered route.
                if (!this._routerPath) return;
                if (!window.location.pathname.startsWith(this._routerPath)) return;
                history.replaceState({}, "", `${this._routerPath}/${firstTabPath}`);
            },
        });

        routes.push({
            path: "**",
            component: async () => (await import("@umbraco-cms/backoffice/router")).UmbRouteNotFoundElement,
        });

        this._routes = routes;
    }

    #pathForTab(tab: TabViewModel): string {
        if (tab.key === ROOT_TAB_KEY) return "root";
        return `tab/${encodeFolderName(tab.name)}`;
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

    override render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._allProperties.length === 0 && this.#initialized) {
            return html`<uui-label><em>No properties found for this content type.</em></uui-label>`;
        }

        const showTabs = !!this._routerPath && this._tabs.length > 1;

        return html`
            <div id="header">
                <div id="name-container">
                    <uui-input
                        id="name-input"
                        .value=${this._entityName}
                        @input=${this.#onNameChange}
                        placeholder="Enter a name..."
                        label="Name"
                    ></uui-input>
                </div>
                ${showTabs
                    ? html`
                        <div id="tabs-container">
                            <uui-tab-group>
                                ${repeat(
                                    this._tabs,
                                    (tab) => tab.key,
                                    (tab) => this.#renderTab(tab),
                                )}
                            </uui-tab-group>
                        </div>`
                    : nothing}
            </div>

            <div id="body">
                <umb-property-dataset .value=${this._propertyValues} @change=${this.#onPropertyDatasetChange}>
                    <umb-router-slot
                        .routes=${this._routes}
                        @init=${(e: UmbRouterSlotInitEvent) => {
                            this._routerPath = e.target.absoluteRouterPath;
                        }}
                        @change=${(e: UmbRouterSlotChangeEvent) => {
                            this._activePath = e.target.absoluteActiveViewPath || "";
                        }}
                    ></umb-router-slot>
                </umb-property-dataset>
            </div>
        `;
    }

    #renderTab(tab: TabViewModel) {
        const basePath = (this._routerPath ?? "") + "/";
        const path = this.#pathForTab(tab);
        const fullPath = basePath + path;
        const active = fullPath === this._activePath;
        return html`
            <uui-tab
                label=${tab.name}
                .active=${active}
                href=${fullPath}
            >${tab.name}</uui-tab>
        `;
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
            flex-shrink: 0;
        }

        #name-container {
            padding: var(--uui-size-layout-1);
            border-bottom: 1px solid var(--uui-color-border);
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
            background-color: var(--uui-color-background);
        }
    `;
}

export { UaiCmsMockEntityEditorElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiCmsMockEntityEditorElement;
    }
}

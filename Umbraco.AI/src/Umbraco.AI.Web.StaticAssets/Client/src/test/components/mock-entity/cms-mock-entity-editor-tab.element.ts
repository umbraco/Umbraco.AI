import { css, customElement, html, nothing, property, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbDataPathPropertyValueQuery } from "@umbraco-cms/backoffice/validation";
import type { UmbPropertyTypeModel } from "@umbraco-cms/backoffice/content-type";
import type { GroupViewModel, TabViewModel } from "./types.js";

const elementName = "uai-cms-mock-entity-editor-tab";

/**
 * Routed view for a single tab of the CMS mock entity editor.
 *
 * Mirrors the CMS block-workspace-view-edit-tab pattern: renders a uui-box
 * per group (plus one for root properties if any) and lets the parent
 * umb-property-dataset supply the property dataset + variant contexts.
 *
 * Property data-paths are computed as invariant (null culture/segment)
 * because mock entities in the test workspace do not vary.
 */
@customElement(elementName)
export class UaiCmsMockEntityEditorTabElement extends UmbLitElement {
    @property({ attribute: false })
    tab?: TabViewModel;

    override render() {
        const tab = this.tab;
        if (!tab) return nothing;

        return html`
            ${tab.rootProperties.length > 0
                ? html`<uui-box>${this.#renderProperties(tab.rootProperties)}</uui-box>`
                : nothing}
            ${repeat(
                tab.groups,
                (group) => group.key,
                (group) => this.#renderGroup(group),
            )}
        `;
    }

    #renderGroup(group: GroupViewModel) {
        return html`
            <uui-box .headline=${group.name}>
                ${this.#renderProperties(group.properties)}
            </uui-box>
        `;
    }

    #renderProperties(properties: UmbPropertyTypeModel[]) {
        return repeat(
            properties,
            (prop) => prop.alias,
            (prop) => html`
                <umb-property-type-based-property
                    data-path=${this.#dataPathFor(prop.alias)}
                    .property=${prop}
                ></umb-property-type-based-property>
            `,
        );
    }

    #dataPathFor(alias: string): string {
        // Mock entities are invariant; match the CMS block-workspace-view-edit-property
        // pattern so local validation reporting resolves correctly for nested editors.
        return `$.values[${UmbDataPathPropertyValueQuery({ alias, culture: null, segment: null })}].value`;
    }

    static override styles = css`
        :host {
            display: block;
            padding: var(--uui-size-layout-1);
        }
        uui-box {
            --uui-box-default-padding: 0 var(--uui-size-space-5);
        }
        uui-box + uui-box {
            margin-top: var(--uui-size-layout-1);
        }
    `;
}

export { UaiCmsMockEntityEditorTabElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiCmsMockEntityEditorTabElement;
    }
}

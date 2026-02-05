import {
    css,
    customElement,
    html,
    property,
    repeat,
    state,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { UmbFormControlMixin } from '@umbraco-cms/backoffice/validation';
import { UaiToolRepository, type ToolScopeItemResponseModel, toCamelCase } from '@umbraco-ai/core';

const elementName = 'uai-tool-scope-permissions';

interface UaiToolScopeItemModel {
    id: string;
    icon: string;
    name: string;
    description: string;
    domain: string;
}

interface UaiToolScopeGroup {
    domain: string;
    scopes: UaiToolScopeItemModel[];
}

@customElement(elementName)
export class UaiToolScopePermissionsElement extends UmbFormControlMixin<
    string[] | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    #toolRepository = new UaiToolRepository(this);

    /**
     * Readonly mode - cannot toggle permissions.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * The selected tool scope IDs.
     */
    override set value(val: string[] | undefined) {
        this._selection = val ? [...val] : [];
    }
    override get value(): string[] | undefined {
        return this._selection.length > 0 ? this._selection : undefined;
    }

    @state()
    private _selection: string[] = [];

    @state()
    private _groups: UaiToolScopeGroup[] = [];

    @state()
    private _loading = false;

    override connectedCallback() {
        super.connectedCallback();
        this.#loadScopes();
    }

    async #loadScopes() {
        this._loading = true;

        const response = await this.#toolRepository.getToolScopes();

        if (!response.error && response.data) {
            // Map API scopes to internal model with localization
            const scopes: UaiToolScopeItemModel[] = response.data.map((scope: ToolScopeItemResponseModel) => {
                const camelCaseId = toCamelCase(scope.id);
                return {
                    id: scope.id,
                    icon: scope.icon,
                    domain: scope.domain || 'General',
                    name: this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.id,
                    description: this.localize.term(`uaiToolScope_${camelCaseId}Description`) || '',
                };
            });

            // Group by domain
            const groupMap = new Map<string, UaiToolScopeItemModel[]>();
            for (const scope of scopes) {
                const existing = groupMap.get(scope.domain) || [];
                existing.push(scope);
                groupMap.set(scope.domain, existing);
            }

            // Convert to array of groups
            this._groups = Array.from(groupMap.entries())
                .map(([domain, scopes]) => ({ domain, scopes }))
                .sort((a, b) => a.domain.localeCompare(b.domain));
        }

        this._loading = false;
    }

    #isSelected(scopeId: string): boolean {
        return this._selection.includes(scopeId);
    }

    #toggleScope(scopeId: string) {
        if (this.readonly) return;

        const wasSelected = this.#isSelected(scopeId);
        let changed = false;

        if (wasSelected) {
            // Remove from selection
            this._selection = this._selection.filter(id => id !== scopeId);
            changed = true;
        } else {
            // Add to selection
            this._selection = [...this._selection, scopeId];
            changed = true;
        }

        // Only dispatch change event if selection actually changed
        if (changed) {
            this.dispatchEvent(new UmbChangeEvent());
        }
    }

    #onToggle(scopeId: string, event: Event) {
        const target = event.target as HTMLInputElement;
        const isChecked = target.checked;
        const wasSelected = this.#isSelected(scopeId);
        let changed = false;

        if (isChecked && !wasSelected) {
            // Add to selection (only if not already selected)
            this._selection = [...this._selection, scopeId];
            changed = true;
        } else if (!isChecked && wasSelected) {
            // Remove from selection (only if currently selected)
            this._selection = this._selection.filter(id => id !== scopeId);
            changed = true;
        }

        // Only dispatch change event if selection actually changed
        if (changed) {
            this.dispatchEvent(new UmbChangeEvent());
        }
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._groups.length === 0) {
            return html`<div class="empty">${this.localize.term('uaiAgent_noToolScopesAvailable')}</div>`;
        }

        return html`
            <div class="container">
                ${repeat(
                    this._groups,
                    (group) => group.domain,
                    (group) => this.#renderGroup(group),
                )}
            </div>
        `;
    }

    #renderGroup(group: UaiToolScopeGroup) {
        const camelCaseDomain = toCamelCase(group.domain);
        const localizedDomain = this.localize.term(`uaiToolScopeDomain_${camelCaseDomain}`) || group.domain;

        return html`
            <div class="group">
                <div class="group-header">${localizedDomain}</div>
                <div class="group-items">
                    ${repeat(
                        group.scopes,
                        (scope) => scope.id,
                        (scope) => this.#renderScope(scope),
                    )}
                </div>
            </div>
        `;
    }

    #renderScope(scope: UaiToolScopeItemModel) {
        const isChecked = this.#isSelected(scope.id);
        const scopeId = `scope-${scope.id}`;

        return html`
            <div class="scope-item">
                <uui-toggle
                    id=${scopeId}
                    ?checked=${isChecked}
                    ?disabled=${this.readonly}
                    @change=${(e: Event) => this.#onToggle(scope.id, e)}
                    label=${scope.name}>
                </uui-toggle>
                <label for=${scopeId} class="scope-label" @click=${() => this.#toggleScope(scope.id)}>
                    <div class="scope-name">${scope.name}</div>
                    <div class="scope-description">${scope.description}</div>
                </label>
            </div>
        `;
    }

    static override styles = [
        css`
            :host {
                display: block;
            }

            .container {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-6);
            }

            .group {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-3);
            }

            .group-header {
                font-weight: 700;
                font-size: 14px;
                color: var(--uui-color-text-alt);
                padding-bottom: var(--uui-size-space-3);
                margin-bottom: var(--uui-size-space-3);
                border-bottom: 1px solid var(--uui-color-border);
            }

            .group-items {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-4);
            }

            .scope-item {
                display: grid;
                grid-template-columns: auto 1fr;
                gap: var(--uui-size-space-4);
                align-items: start;
            }

            .scope-label {
                display: flex;
                flex-direction: column;
                gap: var(--uui-size-space-1);
                cursor: pointer;
            }

            .scope-name {
                font-weight: 600;
                font-size: 14px;
                color: var(--uui-color-text);
            }

            .scope-description {
                font-size: 12px;
                color: var(--uui-color-text-alt);
                line-height: 1.4;
            }

            .empty {
                padding: var(--uui-size-space-4);
                text-align: center;
                color: var(--uui-color-text-alt);
            }

            :host([readonly]) .scope-label {
                cursor: default;
            }
        `,
    ];
}

export default UaiToolScopePermissionsElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiToolScopePermissionsElement;
    }
}

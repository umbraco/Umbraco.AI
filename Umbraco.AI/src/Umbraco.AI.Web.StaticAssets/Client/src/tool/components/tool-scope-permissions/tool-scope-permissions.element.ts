import { css, customElement, html, property, repeat, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { type ToolScopeItemResponseModel } from "../../repository/tool.repository.js";
import { UaiToolController } from "../../controllers/tool.controller.js";
import { toCamelCase } from "../../utils.js";

const elementName = "uai-tool-scope-permissions";

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
    #toolController = new UaiToolController(this);

    /**
     * Readonly mode - cannot toggle permissions.
     */
    @property({ type: Boolean, reflect: true })
    public readonly = false;

    /**
     * Hide scopes that have no tools.
     */
    @property({ type: Boolean })
    hideEmptyScopes = false;

    /**
     * The selected tool scope IDs.
     */
    override set value(val: string[] | undefined) {
        this._selection = val ? [...val] : [];
        this.requestUpdate();
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

    @state()
    private _toolCounts: Record<string, number> = {};

    override connectedCallback() {
        super.connectedCallback();
        this.#loadToolCounts();
    }

    override updated(changedProperties: Map<string, unknown>): void {
        super.updated(changedProperties);
        if (changedProperties.has('hideEmptyScopes')) {
            this.#loadScopes(); // Re-filter with new setting
        }
    }

    async #loadToolCounts() {
        this._toolCounts = await this.#toolController.getToolCountsByScope();
        // Load scopes after counts are available to ensure proper filtering
        this.#loadScopes();
    }

    async #loadScopes() {
        this._loading = true;

        const response = await this.#toolController.getToolScopes();

        if (!response.error && response.data) {
            // Map API scopes to internal model with localization
            let scopes: UaiToolScopeItemModel[] = response.data.map((scope: ToolScopeItemResponseModel) => {
                const camelCaseId = toCamelCase(scope.id);
                return {
                    id: scope.id,
                    icon: scope.icon,
                    domain: scope.domain || "General",
                    name: this.localize.term(`uaiToolScope_${camelCaseId}Label`) || scope.id,
                    description: this.localize.term(`uaiToolScope_${camelCaseId}Description`) || "",
                };
            });

            // Filter empty scopes if hideEmptyScopes is true
            if (this.hideEmptyScopes) {
                scopes = scopes.filter(scope => (this._toolCounts[scope.id] ?? 0) > 0);
            }

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
            this._selection = this._selection.filter((id) => id !== scopeId);
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
            this._selection = this._selection.filter((id) => id !== scopeId);
            changed = true;
        }

        // Only dispatch change event if selection actually changed
        if (changed) {
            this.dispatchEvent(new UmbChangeEvent());
        }
    }

    #getTotalScopeCount(): number {
        return this._groups.reduce((total, group) => total + group.scopes.length, 0);
    }

    #getSelectAllState(): { checked: boolean; indeterminate: boolean } {
        const totalScopes = this.#getTotalScopeCount();

        if (totalScopes === 0) {
            return { checked: false, indeterminate: false };
        }

        // Count only selected scopes that are actually visible (not filtered out)
        const visibleScopeIds = new Set(
            this._groups.flatMap((group) => group.scopes.map((scope) => scope.id))
        );
        const visibleSelectedCount = this._selection.filter(id => visibleScopeIds.has(id)).length;

        if (visibleSelectedCount === 0) {
            return { checked: false, indeterminate: false };
        } else if (visibleSelectedCount === totalScopes) {
            return { checked: true, indeterminate: false };
        } else {
            return { checked: false, indeterminate: true };
        }
    }

    #onSelectAllToggle(event: Event) {
        if (this.readonly) return;

        const target = event.target as HTMLInputElement;
        const isChecking = target.checked;

        if (isChecking) {
            // Select all scopes
            this._selection = this._groups.flatMap((group) => group.scopes.map((scope) => scope.id));
        } else {
            // Deselect all scopes
            this._selection = [];
        }

        this.dispatchEvent(new UmbChangeEvent());
    }

    #handleSelectAllLabelClick() {
        if (this.readonly) return;

        const state = this.#getSelectAllState();
        const shouldCheck = !state.checked;

        if (shouldCheck) {
            // Select all scopes
            this._selection = this._groups.flatMap((group) => group.scopes.map((scope) => scope.id));
        } else {
            // Deselect all scopes
            this._selection = [];
        }

        this.dispatchEvent(new UmbChangeEvent());
    }

    #renderSelectAll() {
        const state = this.#getSelectAllState();

        return html`
            <div class="select-all-container">
                <uui-toggle
                    ?checked=${state.checked}
                    ?indeterminate=${state.indeterminate}
                    ?disabled=${this.readonly}
                    @change=${this.#onSelectAllToggle}
                    label=${this.localize.term("uaiToolScopes_selectAll")}
                >
                </uui-toggle>
                <label class="select-all-label" @click=${this.#handleSelectAllLabelClick}>
                    <div class="select-all-text">${this.localize.term("uaiToolScopes_selectAll")}</div>
                    <div class="scope-description">${this.localize.term("uaiToolScopes_selectAllDescription")}</div>
                </label>
            </div>
        `;
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader-bar></uui-loader-bar>`;
        }

        if (this._groups.length === 0) {
            return html`<div class="empty">${this.localize.term("uaiAgent_noToolScopesAvailable")}</div>`;
        }

        return html`
            <div class="container">
                ${this.#renderSelectAll()}
                <div class="groups-container">
                    ${repeat(
                        this._groups,
                        (group) => group.domain,
                        (group) => this.#renderGroup(group),
                    )}
                </div>
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
        const toolCount = this._toolCounts[scope.id] ?? 0;
        const toolCountLabel = this.localize.term("uaiGeneral_toolCount", toolCount);

        return html`
            <div class="scope-item">
                <uui-toggle
                    id=${scopeId}
                    ?checked=${isChecked}
                    ?disabled=${this.readonly}
                    @change=${(e: Event) => this.#onToggle(scope.id, e)}
                    label=${scope.name}
                >
                </uui-toggle>
                <label for=${scopeId} class="scope-label" @click=${() => this.#toggleScope(scope.id)}>
                    <div class="scope-name">
                        ${scope.name}
                    </div>
                    <div class="scope-description">${scope.description}</div>
                </label>
                <uui-tag look="secondary" style="margin-right: var(--uui-size-space-2)">${toolCountLabel}</uui-tag>
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
            }

            .select-all-container {
                display: flex;
                gap: var(--uui-size-space-4);
                align-items: start;
                padding-bottom: var(--uui-size-space-4);
                margin-bottom: var(--uui-size-space-5);
                border-bottom: 2px solid var(--uui-color-emphasis);
            }

            .select-all-label {
                display: flex;
                flex: 1;
                flex-direction: column;
                cursor: pointer;
            }

            .select-all-text {
                font-weight: 700;
                font-size: 14px;
                color: var(--uui-color-text);
            }

            .groups-container {
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
                display: flex;
                gap: var(--uui-size-space-4);
                align-items: start;
                margin-bottom: var(--uui-size-space-2);
            }

            .scope-label {
                display: flex;
                flex: 1;
                flex-direction: column;
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

            :host([readonly]) .select-all-label {
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

/**
 * Entity Selector Element
 *
 * Displays detected entities and allows user to select which entity
 * should provide context for the AI conversation.
 *
 * - Single entity: Shows entity name badge
 * - Multiple entities: Shows selectable chips for each entity
 * - Auto-selects deepest (most recent) entity by default
 */

import { customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { html, css, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UAI_COPILOT_CONTEXT, type UaiCopilotContext } from "../../copilot.context.js";
import type { UaiDetectedEntity } from "@umbraco-ai/core";

@customElement("uai-entity-selector")
export class UaiEntitySelectorElement extends UmbLitElement {
    #copilotContext?: UaiCopilotContext;

    @state()
    private _detectedEntities: UaiDetectedEntity[] = [];

    @state()
    private _selectedKey?: string;

    constructor() {
        super();
        this.consumeContext(UAI_COPILOT_CONTEXT, (context) => {
            if (context) {
                this.#copilotContext = context;

                // Subscribe to detected entities
                this.observe(context.detectedEntities$, (entities) => {
                    this._detectedEntities = entities;

                    // Auto-select deepest if no selection or selection no longer exists
                    if (!this._selectedKey || !entities.find((e) => e.key === this._selectedKey)) {
                        const newKey = entities[entities.length - 1]?.key;
                        this._selectedKey = newKey;
                        this.#notifySelection(newKey);
                    }
                });

                // Subscribe to selected entity (in case it changes externally)
                this.observe(context.selectedEntity$, (entity) => {
                    if (entity && entity.key !== this._selectedKey) {
                        this._selectedKey = entity.key;
                    }
                });
            }
        });
    }

    #notifySelection(key: string | undefined): void {
        this.#copilotContext?.setSelectedEntityKey(key);
    }

    #handleSelect(key: string): void {
        this._selectedKey = key;
        this.#notifySelection(key);
    }

    #getEntityIcon(entity: UaiDetectedEntity): string {
        // Use adapter-provided icon if available, otherwise fall back to type-based default
        if (entity.icon) {
            return entity.icon;
        }
        switch (entity.entityContext.entityType) {
            case "document":
                return "icon-document";
            case "media":
                return "icon-picture";
            default:
                return "icon-box";
        }
    }

    override render() {
        if (this._detectedEntities.length === 0) {
            return nothing;
        }

        // Single entity: simple display
        if (this._detectedEntities.length === 1) {
            const entity = this._detectedEntities[0];
            return html`
                <div class="entity-selector single">
                    <uui-icon name=${this.#getEntityIcon(entity)}></uui-icon>
                    <span class="entity-name">${entity.name}</span>
                </div>
            `;
        }

        // Multiple entities: selectable chips
        return html`
            <div class="entity-selector multiple">
                <span class="label">Context:</span>
                <div class="chips">
                    ${this._detectedEntities.map(
                        (entity) => html`
                            <button
                                class="entity-chip ${entity.key === this._selectedKey ? "selected" : ""}"
                                @click=${() => this.#handleSelect(entity.key)}
                                title=${entity.entityContext.entityType}
                            >
                                <uui-icon name=${this.#getEntityIcon(entity)}></uui-icon>
                                <span>${entity.name}</span>
                            </button>
                        `,
                    )}
                </div>
            </div>
        `;
    }

    static override styles = css`
        :host {
            display: block;
        }

        .entity-selector {
            padding: var(--uui-size-space-3) var(--uui-size-space-4);
            background: var(--uui-color-surface-alt);
            border-bottom: 1px solid var(--uui-color-border);
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-2);
            font-size: 0.875rem;
        }

        .entity-selector.single {
            color: var(--uui-color-text-alt);
        }

        .entity-selector.single uui-icon {
            font-size: 14px;
        }

        .entity-name {
            font-weight: 500;
        }

        .label {
            color: var(--uui-color-text-alt);
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        .chips {
            display: flex;
            gap: var(--uui-size-space-2);
            flex-wrap: wrap;
        }

        .entity-chip {
            display: inline-flex;
            align-items: center;
            gap: var(--uui-size-space-1);
            padding: var(--uui-size-space-1) var(--uui-size-space-3);
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            cursor: pointer;
            font-size: 0.8125rem;
            color: var(--uui-color-text);
            transition:
                background-color 0.15s ease,
                border-color 0.15s ease;
        }

        .entity-chip:hover {
            background: var(--uui-color-surface-emphasis);
        }

        .entity-chip.selected {
            background: var(--uui-color-selected);
            border-color: var(--uui-color-selected-emphasis);
            color: var(--uui-color-selected-contrast);
        }

        .entity-chip uui-icon {
            font-size: 12px;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "uai-entity-selector": UaiEntitySelectorElement;
    }
}

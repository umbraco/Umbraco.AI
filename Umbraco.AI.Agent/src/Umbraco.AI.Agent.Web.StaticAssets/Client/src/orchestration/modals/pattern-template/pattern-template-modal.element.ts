import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbTextStyles } from "@umbraco-cms/backoffice/style";
import type {
    UaiOrchestrationPatternTemplateModalData,
    UaiOrchestrationPatternTemplateModalValue,
} from "./pattern-template-modal.token.js";
import type { OrchestrationPatternTemplate } from "./pattern-template-modal.token.js";
import { PATTERN_TEMPLATES, generateTemplateGraph } from "./pattern-templates.js";

/**
 * Modal for selecting an orchestration pattern template.
 * Displayed as visual cards for each MAF orchestration pattern.
 */
@customElement("uai-orchestration-pattern-template-modal")
export class UaiOrchestrationPatternTemplateModalElement extends UmbModalBaseElement<
    UaiOrchestrationPatternTemplateModalData,
    UaiOrchestrationPatternTemplateModalValue
> {
    @state()
    private _selectedTemplate?: OrchestrationPatternTemplate;

    #onTemplateSelected(templateId: OrchestrationPatternTemplate) {
        this._selectedTemplate = templateId;
        const graph = generateTemplateGraph(templateId);
        this.value = { graph, templateName: templateId };
        this.modalContext?.submit();
    }

    render() {
        return html`
            <umb-body-layout headline="Choose a Pattern Template">
                <div id="main">
                    <p class="description">
                        Select a starting template for your orchestration. The graph can be freely
                        modified after creation.
                    </p>
                    <div class="template-grid">
                        ${PATTERN_TEMPLATES.map(
                            (template) => html`
                                <button
                                    class="template-card ${this._selectedTemplate === template.id
                                        ? "selected"
                                        : ""}"
                                    @click=${() => this.#onTemplateSelected(template.id)}
                                >
                                    <uui-icon name=${template.icon}></uui-icon>
                                    <strong>${template.label}</strong>
                                    <span class="template-description">${template.description}</span>
                                </button>
                            `,
                        )}
                    </div>
                </div>
                <div slot="actions">
                    <uui-button @click=${this._rejectModal} label="Cancel"></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static styles = [
        UmbTextStyles,
        css`
            .description {
                margin-bottom: var(--uui-size-space-5);
                color: var(--uui-color-text-alt);
            }

            .template-grid {
                display: grid;
                grid-template-columns: repeat(3, 1fr);
                gap: var(--uui-size-space-4);
            }

            .template-card {
                display: flex;
                flex-direction: column;
                align-items: center;
                gap: var(--uui-size-space-3);
                padding: var(--uui-size-space-5);
                border: 2px solid var(--uui-color-border);
                border-radius: var(--uui-border-radius);
                background: var(--uui-color-surface);
                cursor: pointer;
                text-align: center;
                transition:
                    border-color 0.15s,
                    box-shadow 0.15s;
            }

            .template-card:hover {
                border-color: var(--uui-color-interactive-emphasis);
                box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
            }

            .template-card.selected {
                border-color: var(--uui-color-positive);
            }

            .template-card uui-icon {
                font-size: 32px;
                color: var(--uui-color-interactive);
            }

            .template-description {
                font-size: var(--uui-type-small-size);
                color: var(--uui-color-text-alt);
            }
        `,
    ];
}

export default UaiOrchestrationPatternTemplateModalElement;

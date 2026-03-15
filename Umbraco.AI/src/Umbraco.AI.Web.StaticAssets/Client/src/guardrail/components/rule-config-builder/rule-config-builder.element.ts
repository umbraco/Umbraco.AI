import { css, html, customElement, property, state, repeat } from "@umbraco-cms/backoffice/external/lit";
import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UmbChangeEvent } from "@umbraco-cms/backoffice/event";
import { UmbFormControlMixin } from "@umbraco-cms/backoffice/validation";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UAI_ITEM_PICKER_MODAL } from "../../../core/modals/item-picker/item-picker-modal.token.js";
import type { UaiPickableItemModel } from "../../../core/modals/item-picker/types.js";
import { UaiSelectedEvent } from "../../../core/events/selected.event.js";
import { UAI_GUARDRAIL_RULE_CONFIG_EDITOR_MODAL } from "../../modals/rule-config-editor/index.js";
import type { UaiGuardrailRuleConfig, UaiGuardrailRuleModel } from "../../types.js";
import { getRuleSummary } from "../../types.js";
import { UaiGuardrailEvaluatorItemRepository } from "../../repository/evaluator/guardrail-evaluator-item.repository.js";
import type { GuardrailEvaluatorInfoApiModel } from "../../api.js";

@customElement("uai-guardrail-rule-config-builder")
export class UaiGuardrailRuleConfigBuilderElement extends UmbFormControlMixin<
    UaiGuardrailRuleConfig[] | undefined,
    typeof UmbLitElement,
    undefined
>(UmbLitElement, undefined) {
    @property({ type: Array })
    set rules(value: UaiGuardrailRuleModel[]) {
        this.value = value.length > 0 ? (value as UaiGuardrailRuleConfig[]) : undefined;
    }
    get rules(): UaiGuardrailRuleConfig[] {
        return this.value ?? [];
    }

    @state()
    private _evaluators: GuardrailEvaluatorInfoApiModel[] = [];

    override async connectedCallback() {
        super.connectedCallback();
        await this.#fetchEvaluators();
    }

    async #fetchEvaluators() {
        const repository = new UaiGuardrailEvaluatorItemRepository(this);
        const { data, error } = await repository.requestItems();
        if (error) {
            console.error("Failed to fetch evaluator types:", error);
        } else {
            this._evaluators = data ?? [];
        }
    }

    async #onAdd() {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        // Open evaluator type picker
        const typeModal = modalManager.open(this, UAI_ITEM_PICKER_MODAL, {
            data: {
                fetchItems: () => this.#getEvaluatorItems(),
                selectionMode: "single",
                title: "Select Evaluator",
                autoSubmit: false,
            },
        });

        // Listen for selection event (picker stays open)
        typeModal.addEventListener(UaiSelectedEvent.TYPE, async (e: Event) => {
            const selectedEvent = e as UaiSelectedEvent;
            const selectedType = selectedEvent.item as UaiPickableItemModel;

            // Open config editor over the picker (picker stays open)
            const configModal = modalManager.open(this, UAI_GUARDRAIL_RULE_CONFIG_EDITOR_MODAL, {
                data: {
                    evaluatorId: selectedType.value,
                    evaluatorName: selectedType.label,
                    existingRule: undefined,
                },
            });

            try {
                const configResult = await configModal.onSubmit();

                // Config submitted - close picker and add rule
                typeModal.reject();
                this.value = [...this.rules, configResult.rule];
                this.dispatchEvent(new UmbChangeEvent());
            } catch {
                // Config cancelled - picker remains open so user can select different type
            }
        });
    }

    async #onEdit(rule: UaiGuardrailRuleConfig) {
        const modalManager = await this.getContext(UMB_MODAL_MANAGER_CONTEXT);
        if (!modalManager) return;

        // Get evaluator type name
        const typeName = this._evaluators.find((t) => t.id === rule.evaluatorId)?.name;

        // Open config editor directly (skip type picker)
        const modal = modalManager.open(this, UAI_GUARDRAIL_RULE_CONFIG_EDITOR_MODAL, {
            data: {
                evaluatorId: rule.evaluatorId,
                evaluatorName: typeName || rule.evaluatorId,
                existingRule: rule,
            },
        });

        try {
            const result = await modal.onSubmit();

            // Update in list and notify
            this.value = this.rules.map((r) => (r.id === rule.id ? result.rule : r));
            this.dispatchEvent(new UmbChangeEvent());
        } catch {
            // User cancelled
        }
    }

    #onRemove(ruleId: string) {
        const updated = this.rules.filter((r) => r.id !== ruleId);
        this.value = updated.length > 0 ? updated : undefined;
        this.dispatchEvent(new UmbChangeEvent());
    }

    async #getEvaluatorItems(): Promise<UaiPickableItemModel[]> {
        return this._evaluators.map((type) => ({
            icon: "icon-shield color-blue",
            value: type.id,
            label: type.name,
            description: type.description || undefined,
            meta: { type: type.type },
        }));
    }

    #getRuleDetail(rule: UaiGuardrailRuleConfig): string {
        const typeName = this._evaluators.find((t) => t.id === rule.evaluatorId)?.name;
        return getRuleSummary(rule, typeName);
    }

    override render() {
        return html`
            <uui-ref-list>
                ${repeat(
                    this.rules,
                    (rule) => rule.id,
                    (rule) => html`
                        <uui-ref-node
                            name=${rule.name || "Unnamed rule"}
                            detail=${this.#getRuleDetail(rule)}
                        >
                            <umb-icon slot="icon" name="icon-shield color-blue"></umb-icon>
                            <uui-action-bar slot="actions">
                                <uui-button @click=${() => this.#onEdit(rule)} label="Edit">
                                    <uui-icon name="icon-edit"></uui-icon>
                                </uui-button>
                                <uui-button @click=${() => this.#onRemove(rule.id)} label="Remove">
                                    <uui-icon name="icon-trash"></uui-icon>
                                </uui-button>
                            </uui-action-bar>
                        </uui-ref-node>
                    `,
                )}
            </uui-ref-list>
            <uui-button class="add-btn" look="placeholder" label="Add Rule" @click=${this.#onAdd}>
                <uui-icon name="icon-add"></uui-icon>
                Add Rule
            </uui-button>
        `;
    }

    static override styles = [
        css`
            .add-btn {
                width: 100%;
            }
        `,
    ];
}

export default UaiGuardrailRuleConfigBuilderElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-guardrail-rule-config-builder": UaiGuardrailRuleConfigBuilderElement;
    }
}

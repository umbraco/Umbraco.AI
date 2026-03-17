import { css, html, customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import type {
    UaiGuardrailRuleConfigEditorModalData,
    UaiGuardrailRuleConfigEditorModalValue,
} from "./guardrail-rule-config-editor-modal.token.js";
import type { UaiGuardrailRuleConfig } from "../../types.js";
import { createEmptyRuleConfig } from "../../types.js";
import { UaiGuardrailEvaluatorItemRepository } from "../../repository/evaluator/guardrail-evaluator-item.repository.js";
import type { UaiModelEditorChangeEventDetail } from "../../../core/components/exports.js";
import type { GuardrailEvaluatorInfoModel } from "../../../api/types.gen.js";

@customElement("uai-guardrail-rule-config-editor-modal")
export class UaiGuardrailRuleConfigEditorModalElement extends UmbModalBaseElement<
    UaiGuardrailRuleConfigEditorModalData,
    UaiGuardrailRuleConfigEditorModalValue
> {
    @state()
    private _rule: UaiGuardrailRuleConfig = createEmptyRuleConfig();

    @state()
    private _evaluator: GuardrailEvaluatorInfoModel | null = null;

    @state()
    private _loading = true;

    @state()
    private _configModel: Record<string, unknown> = {};

    override async firstUpdated() {
        // Initialize from existing rule or create new
        if (this.data?.existingRule) {
            this._rule = { ...this.data.existingRule };
            this._configModel = this._rule.config ? { ...this._rule.config } : {};
        } else {
            this._rule = createEmptyRuleConfig();
            this._rule.evaluatorId = this.data?.evaluatorId || "";
            this._configModel = {};
        }

        // Fetch evaluator info
        await this.#fetchEvaluatorInfo();
    }

    async #fetchEvaluatorInfo() {
        if (!this.data?.evaluatorId) {
            this._loading = false;
            return;
        }

        const repository = new UaiGuardrailEvaluatorItemRepository(this);
        const { data, error } = await repository.requestItems();

        if (error) {
            console.error("Error fetching evaluator info:", error);
        } else if (data) {
            this._evaluator = data.find((e) => e.id === this.data?.evaluatorId) || null;
        }

        // Reset Redact action if evaluator doesn't support it
        if (this._rule.action === "Redact" && !this._evaluator?.supportsRedaction) {
            this._rule = { ...this._rule, action: "Block" };
        }

        this._loading = false;
    }

    #onNameChange(e: Event) {
        const input = e.target as HTMLInputElement;
        this._rule = { ...this._rule, name: input.value };
    }

    #onPhaseChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this._rule = {
            ...this._rule,
            phase: select.value as "PreGenerate" | "PostGenerate",
        };
    }

    #onActionChange(e: Event) {
        const select = e.target as HTMLSelectElement;
        this._rule = {
            ...this._rule,
            action: select.value as "Block" | "Warn" | "Redact",
        };
    }

    get #actionOptions() {
        const options: Array<{ value: string; name: string; selected: boolean }> = [
            {
                value: "Block",
                name: "Block",
                selected: this._rule.action === "Block",
            },
            {
                value: "Warn",
                name: "Warn",
                selected: this._rule.action === "Warn",
            },
        ];

        if (this._evaluator?.supportsRedaction) {
            options.push({
                value: "Redact",
                name: "Redact",
                selected: this._rule.action === "Redact",
            });
        }

        return options;
    }

    #onConfigChange(e: CustomEvent<UaiModelEditorChangeEventDetail>) {
        this._configModel = e.detail.model;
    }

    #onSubmit(e: Event) {
        e.preventDefault();

        // Validate required fields
        if (!this._rule.name.trim()) {
            return;
        }

        // Serialize config model to rule config
        const finalRule: UaiGuardrailRuleConfig = {
            ...this._rule,
            config: Object.keys(this._configModel).length > 0 ? this._configModel : null,
        };

        this.value = { rule: finalRule };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    override render() {
        if (this._loading) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <umb-body-layout headline="Configure ${this.data?.evaluatorName} Rule">
                <form id="rule-form" @submit=${this.#onSubmit}>
                    <uui-box headline="General">
                        <umb-property-layout label="Name" description="Display name for this rule">
                            <div slot="editor">
                                <uui-input
                                    id="name"
                                    type="text"
                                    .value=${this._rule.name}
                                    @input=${this.#onNameChange}
                                    placeholder="Enter rule name"
                                    required
                                ></uui-input>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Phase"
                            description="When to evaluate this rule (before or after AI generation)"
                        >
                            <div slot="editor">
                                <uui-select
                                    id="phase"
                                    .value=${this._rule.phase}
                                    .options=${[
                                        {
                                            value: "PreGenerate",
                                            name: "Pre-Generate",
                                            selected: this._rule.phase === "PreGenerate",
                                        },
                                        {
                                            value: "PostGenerate",
                                            name: "Post-Generate",
                                            selected: this._rule.phase === "PostGenerate",
                                        },
                                    ]}
                                    @change=${this.#onPhaseChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label="Action"
                            description="Action to take when the rule is triggered"
                        >
                            <div slot="editor">
                                <uui-select
                                    id="action"
                                    .value=${this._rule.action}
                                    .options=${this.#actionOptions}
                                    @change=${this.#onActionChange}
                                >
                                </uui-select>
                            </div>
                        </umb-property-layout>
                    </uui-box>

                    ${this._evaluator?.configSchema
                        ? html`
                              <uai-model-editor
                                  .schema=${this._evaluator.configSchema}
                                  .model=${this._configModel}
                                  @change=${this.#onConfigChange}
                                  default-group="#uaiFieldGroups_configLabel"
                                  style="margin-top: var(--uui-size-layout-1);"
                              >
                              </uai-model-editor>
                          `
                        : ""}
                </form>

                <div slot="actions">
                    <uui-button label="Cancel" @click=${this.#onCancel}> Cancel </uui-button>
                    <uui-button
                        type="submit"
                        form="rule-form"
                        look="primary"
                        color="positive"
                        label="Save"
                    >
                        Save
                    </uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    static override styles = [
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-box:not(:first-child) {
                margin-top: var(--uui-size-layout-1);
            }

            uui-input,
            uui-select {
                width: 100%;
            }
        `,
    ];
}

export default UaiGuardrailRuleConfigEditorModalElement;

declare global {
    interface HTMLElementTagNameMap {
        "uai-guardrail-rule-config-editor-modal": UaiGuardrailRuleConfigEditorModalElement;
    }
}

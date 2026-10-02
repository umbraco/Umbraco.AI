import { css, html, customElement, state, when, nothing } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalBaseElement } from "@umbraco-cms/backoffice/modal";
import { UmbPropertyEditorConfigCollection } from "@umbraco-cms/backoffice/property-editor";
import type { UmbInputMultipleTextStringElement } from "@umbraco-cms/backoffice/components";
import "@umbraco-cms/backoffice/components";
import type {
    UaiDecisionQuestionConfigModalData,
    UaiDecisionQuestionConfigModalValue,
} from "./decision-question-config-modal.token.js";
import type { UaiDecisionQuestionListItem } from "./types.js";
import "../key-value-list/property-editor-ui-key-value-list.element.js";
import type {
    UaiKeyValueListItem,
    UaiPropertyEditorUIKeyValueListElement,
} from "../key-value-list/property-editor-ui-key-value-list.element.js";

const elementName = "uai-decision-question-config-modal";

const ALIAS_PATTERN = /^[A-Za-z][A-Za-z0-9_]*$/;

/**
 * Per-kind config editor for one question in the `Uai.PropertyEditorUi.DecisionQuestionList`.
 * Opened either from the item picker (new question) or by clicking a row (edit). Mirrors
 * `uai-guardrail-rule-config-editor-modal`'s shape (umb-property-layout fields in a uui-box,
 * submit via a form button, cancel via modalContext.reject()).
 */
@customElement(elementName)
export class UaiDecisionQuestionConfigModalElement extends UmbModalBaseElement<
    UaiDecisionQuestionConfigModalData,
    UaiDecisionQuestionConfigModalValue
> {
    @state()
    private _alias = "";

    @state()
    private _instructions = "";

    @state()
    private _trueCriteria?: string;

    @state()
    private _falseCriteria?: string;

    @state()
    private _threshold = 0.5;

    @state()
    private _options: UaiKeyValueListItem[] = [];

    @state()
    private _levels: string[] = [];

    @state()
    private _aliasError?: string;

    @state()
    private _formError?: string;

    override connectedCallback() {
        super.connectedCallback();

        const existing = this.data?.existingQuestion;
        this._alias = existing?.alias ?? "";
        this._instructions = existing?.instructions ?? "";
        this._trueCriteria = existing?.trueCriteria;
        this._falseCriteria = existing?.falseCriteria;
        this._threshold = existing?.threshold ?? 0.5;
        this._options = existing?.options ?? [];
        this._levels = existing?.levels ?? [];
    }

    #onAliasInput(e: Event) {
        this._alias = (e.target as HTMLInputElement).value;
    }

    #onInstructionsInput(e: Event) {
        this._instructions = (e.target as HTMLTextAreaElement).value;
    }

    #onTrueCriteriaInput(e: Event) {
        this._trueCriteria = (e.target as HTMLInputElement).value;
    }

    #onFalseCriteriaInput(e: Event) {
        this._falseCriteria = (e.target as HTMLInputElement).value;
    }

    #onThresholdInput(e: Event) {
        // A blank input must fail the range check below, not silently become 0 (Number("") === 0,
        // which would save a threshold of 0 — "always yes" — instead of refusing the submit).
        const raw = (e.target as HTMLInputElement).value;
        this._threshold = raw.trim() === "" ? NaN : Number(raw);
    }

    #onOptionsChange(e: Event) {
        this._options = (e.target as UaiPropertyEditorUIKeyValueListElement).value ?? [];
    }

    #onLevelsChange(e: Event) {
        this._levels = (e.target as UmbInputMultipleTextStringElement).items ?? [];
    }

    /** Validates every field and, when valid, builds the flat question to submit. */
    #validate(): UaiDecisionQuestionListItem | undefined {
        this._aliasError = undefined;
        this._formError = undefined;

        const alias = this._alias.trim();
        const otherAliases = this.data?.otherAliases ?? [];
        if (!alias) {
            this._aliasError = this.localize.termOrDefault(
                "uaiDecisionQuestionConfigModal_aliasRequired",
                "Alias is required.",
            );
        } else if (!ALIAS_PATTERN.test(alias)) {
            this._aliasError = this.localize.termOrDefault(
                "uaiDecisionQuestionConfigModal_aliasInvalidFormat",
                "Alias must start with a letter and contain only letters, digits and underscores.",
            );
        } else if (otherAliases.includes(alias)) {
            this._aliasError = this.localize.termOrDefault(
                "uaiDecisionQuestionConfigModal_aliasDuplicate",
                "Alias is already used by another question.",
            );
        }

        const instructions = this._instructions.trim();
        if (!instructions) {
            this._formError = this.localize.termOrDefault(
                "uaiDecisionQuestionConfigModal_instructionsRequired",
                "Instructions are required.",
            );
        } else if (this.data?.kind === "binary") {
            if (Number.isNaN(this._threshold) || this._threshold < 0 || this._threshold > 1) {
                this._formError = this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_thresholdRange",
                    "Threshold must be between 0.0 and 1.0.",
                );
            }
        } else if (this.data?.kind === "choice") {
            const keys = this._options.map((o) => o.key.trim());
            if (this._options.length < 2 || this._options.length > 255) {
                this._formError = this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_optionsRange",
                    "Requires between 2 and 255 options.",
                );
            } else if (keys.some((k) => !k) || new Set(keys).size !== keys.length) {
                this._formError = this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_optionsInvalid",
                    "Every option needs a unique, non-blank key.",
                );
            }
        } else if (this.data?.kind === "score") {
            if (this._levels.length < 2 || this._levels.length > 10) {
                this._formError = this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_levelsRange",
                    "Requires between 2 and 10 levels.",
                );
            } else if (this._levels.some((l) => !l.trim())) {
                this._formError = this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_levelsInvalid",
                    "Every level needs a non-blank label.",
                );
            }
        }

        if (this._aliasError || this._formError) return undefined;

        const question: UaiDecisionQuestionListItem = {
            kind: this.data!.kind,
            alias,
            instructions,
        };

        if (this.data?.kind === "binary") {
            if (this._trueCriteria?.trim()) question.trueCriteria = this._trueCriteria.trim();
            if (this._falseCriteria?.trim()) question.falseCriteria = this._falseCriteria.trim();
            question.threshold = this._threshold;
        } else if (this.data?.kind === "choice") {
            // Keys are trimmed to match what #validate actually checked (bounds, uniqueness).
            question.options = this._options.map((o) => ({ key: o.key.trim(), value: o.value }));
        } else if (this.data?.kind === "score") {
            // Same reasoning as options: store what was validated, not the raw untrimmed input.
            question.levels = this._levels.map((l) => l.trim());
        }

        return question;
    }

    #onSubmit() {
        const question = this.#validate();
        if (!question) return;

        this.value = { question };
        this.modalContext?.submit();
    }

    #onCancel() {
        this.modalContext?.reject();
    }

    override render() {
        const isEditing = !!this.data?.existingQuestion;
        const headline = isEditing
            ? this.localize.termOrDefault("uaiDecisionQuestionConfigModal_headlineEdit", "Edit question")
            : this.localize.termOrDefault("uaiDecisionQuestionConfigModal_headlineAdd", "Add question");

        return html`
            <umb-body-layout headline=${headline}>
                <div>
                    <uui-box>
                        <umb-property-layout
                            label=${this.localize.termOrDefault("uaiDecisionQuestionConfigModal_aliasLabel", "Alias")}
                            description=${this.localize.termOrDefault(
                                "uaiDecisionQuestionConfigModal_aliasDescription",
                                "A short, unique name for this question's output. Letters, digits and underscores, starting with a letter.",
                            )}
                        >
                            <div slot="editor">
                                <uui-input
                                    id="alias"
                                    label=${this.localize.termOrDefault(
                                        "uaiDecisionQuestionConfigModal_aliasLabel",
                                        "Alias",
                                    )}
                                    .value=${this._alias}
                                    @input=${this.#onAliasInput}
                                ></uui-input>
                                ${when(this._aliasError, () => html`<p class="alias-error">${this._aliasError}</p>`)}
                            </div>
                        </umb-property-layout>

                        <umb-property-layout
                            label=${this.localize.termOrDefault(
                                "uaiDecisionQuestionConfigModal_instructionsLabel",
                                "Instructions",
                            )}
                            description=${this.localize.termOrDefault(
                                "uaiDecisionQuestionConfigModal_instructionsDescription",
                                "What to decide.",
                            )}
                        >
                            <div slot="editor">
                                <uui-textarea
                                    id="instructions"
                                    label=${this.localize.termOrDefault(
                                        "uaiDecisionQuestionConfigModal_instructionsLabel",
                                        "Instructions",
                                    )}
                                    .value=${this._instructions}
                                    @input=${this.#onInstructionsInput}
                                ></uui-textarea>
                            </div>
                        </umb-property-layout>

                        ${this.data?.kind === "binary" ? this.#renderBinaryFields() : nothing}
                        ${this.data?.kind === "choice" ? this.#renderChoiceFields() : nothing}
                        ${this.data?.kind === "score" ? this.#renderScoreFields() : nothing}
                    </uui-box>

                    ${when(this._formError, () => html`<p class="form-error">${this._formError}</p>`)}
                </div>

                <div slot="actions">
                    <uui-button
                        label=${this.localize.termOrDefault("uaiDecisionQuestionConfigModal_cancel", "Cancel")}
                        @click=${this.#onCancel}
                    ></uui-button>
                    <uui-button
                        id="btn-submit"
                        look="primary"
                        color="positive"
                        label=${this.localize.termOrDefault("uaiDecisionQuestionConfigModal_save", "Save")}
                        @click=${this.#onSubmit}
                    ></uui-button>
                </div>
            </umb-body-layout>
        `;
    }

    #renderBinaryFields() {
        return html`
            <umb-property-layout
                label=${this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_trueCriteriaLabel",
                    "True criteria",
                )}
                description=${this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_trueCriteriaDescription",
                    'Optional elaboration of what counts as "yes", beyond the instructions.',
                )}
            >
                <div slot="editor">
                    <uui-input
                        id="true-criteria"
                        label=${this.localize.termOrDefault(
                            "uaiDecisionQuestionConfigModal_trueCriteriaLabel",
                            "True criteria",
                        )}
                        .value=${this._trueCriteria ?? ""}
                        @input=${this.#onTrueCriteriaInput}
                    ></uui-input>
                </div>
            </umb-property-layout>

            <umb-property-layout
                label=${this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_falseCriteriaLabel",
                    "False criteria",
                )}
                description=${this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_falseCriteriaDescription",
                    'Optional elaboration of what counts as "no", beyond the instructions.',
                )}
            >
                <div slot="editor">
                    <uui-input
                        id="false-criteria"
                        label=${this.localize.termOrDefault(
                            "uaiDecisionQuestionConfigModal_falseCriteriaLabel",
                            "False criteria",
                        )}
                        .value=${this._falseCriteria ?? ""}
                        @input=${this.#onFalseCriteriaInput}
                    ></uui-input>
                </div>
            </umb-property-layout>

            <umb-property-layout
                label=${this.localize.termOrDefault("uaiDecisionQuestionConfigModal_thresholdLabel", "Threshold")}
                description=${this.localize.termOrDefault(
                    "uaiDecisionQuestionConfigModal_thresholdDescription",
                    'The minimum probability, from 0.0 to 1.0, counted as "yes".',
                )}
            >
                <div slot="editor">
                    <uui-input
                        id="threshold"
                        type="number"
                        label=${this.localize.termOrDefault(
                            "uaiDecisionQuestionConfigModal_thresholdLabel",
                            "Threshold",
                        )}
                        min="0"
                        max="1"
                        step="0.05"
                        .value=${String(this._threshold)}
                        @input=${this.#onThresholdInput}
                    ></uui-input>
                </div>
            </umb-property-layout>
        `;
    }

    #renderChoiceFields() {
        return html`
            <umb-property-layout
                label=${this.localize.termOrDefault("uaiDecisionQuestionConfigModal_optionsLabel", "Options")}
            >
                <div slot="editor">
                    <uai-property-editor-ui-key-value-list
                        id="options"
                        .value=${this._options}
                        .config=${new UmbPropertyEditorConfigCollection([
                            { alias: "min", value: 2 },
                            { alias: "max", value: 255 },
                        ])}
                        @change=${this.#onOptionsChange}
                    ></uai-property-editor-ui-key-value-list>
                </div>
            </umb-property-layout>
        `;
    }

    #renderScoreFields() {
        return html`
            <umb-property-layout
                label=${this.localize.termOrDefault("uaiDecisionQuestionConfigModal_levelsLabel", "Levels")}
            >
                <div slot="editor">
                    <umb-input-multiple-text-string
                        id="levels"
                        .items=${this._levels}
                        .min=${2}
                        .max=${10}
                        @change=${this.#onLevelsChange}
                    ></umb-input-multiple-text-string>
                </div>
            </umb-property-layout>
        `;
    }

    static override styles = [
        css`
            uui-box {
                --uui-box-default-padding: 0 var(--uui-size-space-5);
            }

            uui-input,
            uui-textarea {
                width: 100%;
            }

            .alias-error,
            .form-error {
                color: var(--uui-color-danger);
                font-size: 0.85em;
                margin: var(--uui-size-space-2) 0 0;
            }
        `,
    ];
}

export { UaiDecisionQuestionConfigModalElement as element };

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: UaiDecisionQuestionConfigModalElement;
    }
}

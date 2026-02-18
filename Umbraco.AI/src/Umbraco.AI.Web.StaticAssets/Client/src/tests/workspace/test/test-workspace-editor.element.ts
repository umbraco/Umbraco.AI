import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../../repository/test.repository.js";
import type {
    CreateTestRequestModel,
    UpdateTestRequestModel,
    TestGraderModel,
    TestFeatureInfoModel,
    TestGraderInfoModel,
} from "../../../api/types.gen.js";

/**
 * Test editor workspace for creating and editing AI tests.
 */
@customElement("umbraco-ai-test-workspace-editor")
export class UmbracoAITestWorkspaceEditorElement extends UmbElementMixin(LitElement) {
    @property({ type: String })
    testId?: string;

    @state()
    private _isLoading = true;

    @state()
    private _isSaving = false;

    @state()
    private _testFeatures: TestFeatureInfoModel[] = [];

    @state()
    private _testGraders: TestGraderInfoModel[] = [];

    // Form fields
    @state()
    private _alias = "";

    @state()
    private _name = "";

    @state()
    private _description = "";

    @state()
    private _testTypeId = "";

    @state()
    private _targetId = "";

    @state()
    private _targetIsAlias = false;

    @state()
    private _testCaseJson = "{}";

    @state()
    private _graders: TestGraderModel[] = [];

    @state()
    private _runCount = 3;

    @state()
    private _tags: string[] = [];

    @state()
    private _tagInput = "";

    private _repository!: AITestRepository;

    constructor() {
        super();
        this._repository = new AITestRepository(this);
    }

    async connectedCallback() {
        super.connectedCallback();
        await this._loadMetadata();
        if (this.testId && this.testId !== "create") {
            await this._loadTest();
        } else {
            this._isLoading = false;
        }
    }

    private async _loadMetadata() {
        try {
            this._testFeatures = await this._repository.getAllTestFeatures();
            this._testGraders = await this._repository.getAllTestGraders();
        } catch (error) {
            console.error("Failed to load metadata:", error);
        }
    }

    private async _loadTest() {
        this._isLoading = true;
        try {
            const test = await this._repository.getTestByIdOrAlias(this.testId!);
            if (test) {
                this._alias = test.alias;
                this._name = test.name;
                this._description = test.description || "";
                this._testTypeId = test.testTypeId;
                this._targetId = test.target.targetId;
                this._targetIsAlias = test.target.isAlias;
                this._testCaseJson = test.testCaseJson;
                this._graders = [...test.graders];
                this._runCount = test.runCount;
                this._tags = [...test.tags];
            }
        } catch (error) {
            console.error("Failed to load test:", error);
        } finally {
            this._isLoading = false;
        }
    }

    private async _handleSave() {
        if (!this._alias || !this._name || !this._testTypeId || !this._targetId) {
            alert("Please fill in all required fields");
            return;
        }

        // Validate JSON
        try {
            JSON.parse(this._testCaseJson);
        } catch {
            alert("Invalid test case JSON");
            return;
        }

        this._isSaving = true;
        try {
            const model = {
                alias: this._alias,
                name: this._name,
                description: this._description || undefined,
                testTypeId: this._testTypeId,
                target: {
                    targetId: this._targetId,
                    isAlias: this._targetIsAlias,
                },
                testCaseJson: this._testCaseJson,
                graders: this._graders,
                runCount: this._runCount,
                tags: this._tags,
            };

            if (this.testId && this.testId !== "create") {
                await this._repository.updateTest(this.testId, model as UpdateTestRequestModel);
            } else {
                const id = await this._repository.createTest(model as CreateTestRequestModel);
                window.location.hash = `#/section/ai/workspace/test/edit/${id}`;
            }

            alert("Test saved successfully");
        } catch (error) {
            console.error("Failed to save test:", error);
            alert("Failed to save test. See console for details.");
        } finally {
            this._isSaving = false;
        }
    }

    private _handleAddGrader() {
        const newGrader: TestGraderModel = {
            id: crypto.randomUUID(),
            graderTypeId: this._testGraders[0]?.id || "",
            name: "New Grader",
            description: null,
            configJson: "{}",
            negate: false,
            severity: "Error",
            weight: 1.0,
        };
        this._graders = [...this._graders, newGrader];
    }

    private _handleRemoveGrader(index: number) {
        this._graders = this._graders.filter((_, i) => i !== index);
    }

    private _handleGraderChange(index: number, field: keyof TestGraderModel, value: any) {
        const updated = [...this._graders];
        (updated[index] as any)[field] = value;
        this._graders = updated;
    }

    private _handleAddTag() {
        if (this._tagInput && !this._tags.includes(this._tagInput)) {
            this._tags = [...this._tags, this._tagInput];
            this._tagInput = "";
        }
    }

    private _handleRemoveTag(tag: string) {
        this._tags = this._tags.filter(t => t !== tag);
    }

    render() {
        if (this._isLoading) {
            return html`<div class="loading">Loading...</div>`;
        }

        return html`
            <div class="container">
                <h1>${this.testId === "create" ? "Create" : "Edit"} Test</h1>

                <div class="form">
                    <div class="form-group">
                        <label>Alias *</label>
                        <input
                            type="text"
                            .value=${this._alias}
                            @input=${(e: Event) => (this._alias = (e.target as HTMLInputElement).value)}
                            ?disabled=${this.testId !== "create"}
                        />
                    </div>

                    <div class="form-group">
                        <label>Name *</label>
                        <input
                            type="text"
                            .value=${this._name}
                            @input=${(e: Event) => (this._name = (e.target as HTMLInputElement).value)}
                        />
                    </div>

                    <div class="form-group">
                        <label>Description</label>
                        <textarea
                            .value=${this._description}
                            @input=${(e: Event) => (this._description = (e.target as HTMLTextAreaElement).value)}
                            rows="3"
                        ></textarea>
                    </div>

                    <div class="form-group">
                        <label>Test Type *</label>
                        <select
                            .value=${this._testTypeId}
                            @change=${(e: Event) => (this._testTypeId = (e.target as HTMLSelectElement).value)}
                        >
                            <option value="">Select test type...</option>
                            ${this._testFeatures.map(
                                feature => html`<option value=${feature.id}>${feature.name}</option>`
                            )}
                        </select>
                    </div>

                    <div class="form-group">
                        <label>Target ID/Alias *</label>
                        <div class="target-input">
                            <input
                                type="text"
                                .value=${this._targetId}
                                @input=${(e: Event) => (this._targetId = (e.target as HTMLInputElement).value)}
                                placeholder="Enter prompt or agent ID/alias"
                            />
                            <label class="checkbox-label">
                                <input
                                    type="checkbox"
                                    .checked=${this._targetIsAlias}
                                    @change=${(e: Event) => (this._targetIsAlias = (e.target as HTMLInputElement).checked)}
                                />
                                Is Alias
                            </label>
                        </div>
                    </div>

                    <div class="form-group">
                        <label>Test Case (JSON) *</label>
                        <textarea
                            .value=${this._testCaseJson}
                            @input=${(e: Event) => (this._testCaseJson = (e.target as HTMLTextAreaElement).value)}
                            rows="10"
                            placeholder='{"key": "value"}'
                        ></textarea>
                    </div>

                    <div class="form-group">
                        <label>Run Count</label>
                        <input
                            type="number"
                            .value=${this._runCount.toString()}
                            @input=${(e: Event) => (this._runCount = parseInt((e.target as HTMLInputElement).value) || 1)}
                            min="1"
                            max="100"
                        />
                        <small>Number of times to run this test (for pass@k calculation)</small>
                    </div>

                    <div class="form-group">
                        <label>Tags</label>
                        <div class="tags-input">
                            <input
                                type="text"
                                .value=${this._tagInput}
                                @input=${(e: Event) => (this._tagInput = (e.target as HTMLInputElement).value)}
                                @keypress=${(e: KeyboardEvent) => e.key === "Enter" && this._handleAddTag()}
                                placeholder="Add tag and press Enter"
                            />
                            <button @click=${this._handleAddTag} type="button">Add</button>
                        </div>
                        <div class="tags-list">
                            ${this._tags.map(
                                tag => html`
                                    <span class="tag">
                                        ${tag}
                                        <button @click=${() => this._handleRemoveTag(tag)}>×</button>
                                    </span>
                                `
                            )}
                        </div>
                    </div>

                    <div class="form-group">
                        <div class="section-header">
                            <label>Graders</label>
                            <button @click=${this._handleAddGrader} type="button">+ Add Grader</button>
                        </div>
                        ${this._graders.map((grader, index) => this._renderGrader(grader, index))}
                    </div>

                    <div class="form-actions">
                        <button @click=${this._handleSave} ?disabled=${this._isSaving} class="save-button">
                            ${this._isSaving ? "Saving..." : "Save"}
                        </button>
                        <button @click=${() => window.history.back()} type="button" class="cancel-button">
                            Cancel
                        </button>
                    </div>
                </div>
            </div>
        `;
    }

    private _renderGrader(grader: TestGraderModel, index: number) {
        return html`
            <div class="grader">
                <div class="grader-header">
                    <strong>Grader ${index + 1}</strong>
                    <button @click=${() => this._handleRemoveGrader(index)} type="button" class="remove-button">
                        Remove
                    </button>
                </div>
                <div class="grader-fields">
                    <div class="form-group">
                        <label>Grader Type</label>
                        <select
                            .value=${grader.graderTypeId}
                            @change=${(e: Event) =>
                                this._handleGraderChange(index, "graderTypeId", (e.target as HTMLSelectElement).value)}
                        >
                            ${this._testGraders.map(
                                g => html`<option value=${g.id}>${g.name}</option>`
                            )}
                        </select>
                    </div>
                    <div class="form-group">
                        <label>Name</label>
                        <input
                            type="text"
                            .value=${grader.name}
                            @input=${(e: Event) =>
                                this._handleGraderChange(index, "name", (e.target as HTMLInputElement).value)}
                        />
                    </div>
                    <div class="form-group">
                        <label>Config (JSON)</label>
                        <textarea
                            .value=${grader.configJson || "{}"}
                            @input=${(e: Event) =>
                                this._handleGraderChange(index, "configJson", (e.target as HTMLTextAreaElement).value)}
                            rows="3"
                        ></textarea>
                    </div>
                    <div class="grader-options">
                        <div class="form-group">
                            <label>Severity</label>
                            <select
                                .value=${grader.severity}
                                @change=${(e: Event) =>
                                    this._handleGraderChange(index, "severity", (e.target as HTMLSelectElement).value)}
                            >
                                <option value="Info">Info</option>
                                <option value="Warning">Warning</option>
                                <option value="Error">Error</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Weight</label>
                            <input
                                type="number"
                                .value=${grader.weight.toString()}
                                @input=${(e: Event) =>
                                    this._handleGraderChange(
                                        index,
                                        "weight",
                                        parseFloat((e.target as HTMLInputElement).value) || 1.0
                                    )}
                                min="0"
                                max="1"
                                step="0.1"
                            />
                        </div>
                        <div class="form-group checkbox-group">
                            <label>
                                <input
                                    type="checkbox"
                                    .checked=${grader.negate}
                                    @change=${(e: Event) =>
                                        this._handleGraderChange(index, "negate", (e.target as HTMLInputElement).checked)}
                                />
                                Negate Result
                            </label>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: 20px;
        }

        .loading {
            text-align: center;
            padding: 40px;
        }

        .container {
            max-width: 900px;
            margin: 0 auto;
        }

        h1 {
            margin-bottom: 30px;
        }

        .form {
            background: var(--uui-color-surface);
            padding: 30px;
            border-radius: 8px;
        }

        .form-group {
            margin-bottom: 20px;
        }

        label {
            display: block;
            margin-bottom: 5px;
            font-weight: 500;
        }

        input[type="text"],
        input[type="number"],
        textarea,
        select {
            width: 100%;
            padding: 8px 12px;
            border: 1px solid var(--uui-color-border);
            border-radius: 4px;
            font-family: inherit;
        }

        textarea {
            font-family: monospace;
            resize: vertical;
        }

        small {
            display: block;
            margin-top: 5px;
            color: var(--uui-color-text-alt);
            font-size: 12px;
        }

        .target-input {
            display: flex;
            gap: 10px;
            align-items: center;
        }

        .target-input input {
            flex: 1;
        }

        .checkbox-label {
            display: flex;
            align-items: center;
            gap: 5px;
            font-weight: normal;
            white-space: nowrap;
        }

        .checkbox-label input {
            width: auto;
        }

        .tags-input {
            display: flex;
            gap: 10px;
        }

        .tags-input input {
            flex: 1;
        }

        .tags-input button {
            padding: 8px 16px;
        }

        .tags-list {
            display: flex;
            flex-wrap: wrap;
            gap: 8px;
            margin-top: 10px;
        }

        .tag {
            display: inline-flex;
            align-items: center;
            gap: 5px;
            padding: 4px 10px;
            background: var(--uui-color-surface-alt);
            border-radius: 3px;
            font-size: 14px;
        }

        .tag button {
            background: none;
            border: none;
            cursor: pointer;
            font-size: 18px;
            line-height: 1;
            padding: 0;
            color: var(--uui-color-text-alt);
        }

        .section-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
        }

        .section-header button {
            padding: 6px 12px;
            font-size: 14px;
        }

        .grader {
            background: var(--uui-color-surface-alt);
            padding: 15px;
            border-radius: 4px;
            margin-bottom: 15px;
        }

        .grader-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 15px;
        }

        .remove-button {
            background: var(--uui-color-danger);
            color: white;
            border: none;
            padding: 4px 12px;
            border-radius: 3px;
            cursor: pointer;
            font-size: 12px;
        }

        .grader-fields .form-group {
            margin-bottom: 15px;
        }

        .grader-options {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 15px;
        }

        .checkbox-group label {
            font-weight: normal;
        }

        .form-actions {
            display: flex;
            gap: 10px;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid var(--uui-color-border);
        }

        .save-button {
            padding: 10px 24px;
            background: var(--uui-color-positive);
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }

        .save-button:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }

        .cancel-button {
            padding: 10px 24px;
            background: var(--uui-color-surface-alt);
            border: 1px solid var(--uui-color-border);
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }

        button {
            cursor: pointer;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-test-workspace-editor": UmbracoAITestWorkspaceEditorElement;
    }
}

export default UmbracoAITestWorkspaceEditorElement;

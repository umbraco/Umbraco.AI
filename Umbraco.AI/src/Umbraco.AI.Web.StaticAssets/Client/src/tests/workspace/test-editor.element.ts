import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { customElement, state, property } from '@umbraco-cms/backoffice/external/lit';
import { AITestRepository } from '../repository/test.repository.js';
import type {
    TestResponseModel,
    CreateTestRequestModel,
    UpdateTestRequestModel,
    TestGraderModel,
    TestTargetModel,
    TestCaseModel
} from '../../api/client/index.js';

/**
 * Test editor workspace for creating and editing AI tests.
 */
@customElement('umbraco-ai-test-editor')
export class UmbracoAITestEditorElement extends LitElement {
    @property({ type: String })
    testId?: string;

    @state()
    private _test?: TestResponseModel;

    @state()
    private _isLoading = true;

    @state()
    private _isSaving = false;

    @state()
    private _name = '';

    @state()
    private _alias = '';

    @state()
    private _description = '';

    @state()
    private _testTypeId = 'prompt';

    @state()
    private _runCount = 1;

    @state()
    private _tags: string[] = [];

    @state()
    private _target: TestTargetModel = { profileIdOrAlias: '', contextIds: [] };

    @state()
    private _testCase: TestCaseModel = {};

    @state()
    private _graders: TestGraderModel[] = [];

    private _repository = new AITestRepository(this);

    connectedCallback() {
        super.connectedCallback();
        if (this.testId) {
            this._loadTest();
        } else {
            this._isLoading = false;
        }
    }

    private async _loadTest() {
        if (!this.testId) return;

        this._isLoading = true;
        try {
            const test = await this._repository.getById(this.testId);
            if (test) {
                this._test = test;
                this._name = test.name;
                this._alias = test.alias;
                this._description = test.description || '';
                this._testTypeId = test.testTypeId;
                this._runCount = test.runCount;
                this._tags = [...test.tags];
                this._target = { ...test.target };
                this._testCase = { ...test.testCase };
                this._graders = test.graders.map(g => ({ ...g }));
            }
        } catch (error) {
            console.error('Failed to load test:', error);
            alert('Failed to load test. See console for details.');
        } finally {
            this._isLoading = false;
        }
    }

    private _onNameChange(e: InputEvent) {
        const target = e.target as HTMLInputElement;
        this._name = target.value;
        // Auto-generate alias from name if creating new test
        if (!this.testId && this._alias === '') {
            this._alias = this._name.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '');
        }
    }

    private _onAliasChange(e: InputEvent) {
        const target = e.target as HTMLInputElement;
        this._alias = target.value;
    }

    private _onDescriptionChange(e: InputEvent) {
        const target = e.target as HTMLTextAreaElement;
        this._description = target.value;
    }

    private _onTestTypeChange(e: Event) {
        const target = e.target as HTMLSelectElement;
        this._testTypeId = target.value;
    }

    private _onRunCountChange(e: InputEvent) {
        const target = e.target as HTMLInputElement;
        this._runCount = parseInt(target.value) || 1;
    }

    private _onTagsChange(e: InputEvent) {
        const target = e.target as HTMLInputElement;
        this._tags = target.value.split(',').map(t => t.trim()).filter(t => t);
    }

    private async _onSaveClick() {
        if (!this._name || !this._alias) {
            alert('Name and alias are required.');
            return;
        }

        this._isSaving = true;
        try {
            if (this.testId) {
                // Update existing test
                const model: UpdateTestRequestModel = {
                    name: this._name,
                    description: this._description || undefined,
                    target: this._target,
                    testCase: this._testCase,
                    graders: this._graders,
                    runCount: this._runCount,
                    tags: this._tags
                };
                await this._repository.update(this.testId, model);
                alert('Test updated successfully!');
            } else {
                // Create new test
                const model: CreateTestRequestModel = {
                    alias: this._alias,
                    name: this._name,
                    description: this._description || undefined,
                    testTypeId: this._testTypeId,
                    target: this._target,
                    testCase: this._testCase,
                    graders: this._graders,
                    runCount: this._runCount,
                    tags: this._tags
                };
                const id = await this._repository.create(model);
                alert('Test created successfully!');
                // TODO: Navigate to the test list or the newly created test
                console.log('Created test with ID:', id);
            }
        } catch (error) {
            console.error('Failed to save test:', error);
            alert('Failed to save test. See console for details.');
        } finally {
            this._isSaving = false;
        }
    }

    private _onCancelClick() {
        // TODO: Navigate back to test list
        console.log('Cancel clicked');
    }

    private _onAddGraderClick() {
        this._graders = [
            ...this._graders,
            {
                graderId: 'exact-match',
                config: {},
                weight: 1.0,
                severity: 'error',
                negate: false
            }
        ];
    }

    private _onRemoveGraderClick(index: number) {
        this._graders = this._graders.filter((_, i) => i !== index);
    }

    render() {
        if (this._isLoading) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <div class="test-editor">
                <div class="header">
                    <h1>${this.testId ? 'Edit Test' : 'Create Test'}</h1>
                    <div class="actions">
                        <uui-button
                            label="Cancel"
                            @click=${this._onCancelClick}>
                            Cancel
                        </uui-button>
                        <uui-button
                            label="Save"
                            look="primary"
                            color="positive"
                            ?disabled=${this._isSaving}
                            @click=${this._onSaveClick}>
                            ${this._isSaving ? 'Saving...' : 'Save'}
                        </uui-button>
                    </div>
                </div>

                <div class="form">
                    <div class="form-group">
                        <label>Name *</label>
                        <uui-input
                            label="Name"
                            .value=${this._name}
                            @input=${this._onNameChange}
                            required>
                        </uui-input>
                    </div>

                    <div class="form-group">
                        <label>Alias *</label>
                        <uui-input
                            label="Alias"
                            .value=${this._alias}
                            @input=${this._onAliasChange}
                            ?readonly=${!!this.testId}
                            required>
                        </uui-input>
                        ${!this.testId ? html`<small>Auto-generated from name, can be customized</small>` : ''}
                    </div>

                    <div class="form-group">
                        <label>Description</label>
                        <uui-textarea
                            label="Description"
                            .value=${this._description}
                            @input=${this._onDescriptionChange}>
                        </uui-textarea>
                    </div>

                    ${!this.testId ? html`
                        <div class="form-group">
                            <label>Test Type *</label>
                            <select @change=${this._onTestTypeChange} .value=${this._testTypeId}>
                                <option value="prompt">Prompt Test</option>
                                <option value="agent">Agent Test</option>
                            </select>
                        </div>
                    ` : ''}

                    <div class="form-group">
                        <label>Run Count</label>
                        <uui-input
                            type="number"
                            label="Run Count"
                            .value=${this._runCount.toString()}
                            @input=${this._onRunCountChange}
                            min="1"
                            max="100">
                        </uui-input>
                        <small>Number of times to run the test (for pass@k calculation)</small>
                    </div>

                    <div class="form-group">
                        <label>Tags</label>
                        <uui-input
                            label="Tags"
                            placeholder="tag1, tag2, tag3"
                            .value=${this._tags.join(', ')}
                            @input=${this._onTagsChange}>
                        </uui-input>
                        <small>Comma-separated list of tags</small>
                    </div>

                    <div class="form-section">
                        <h2>Graders</h2>
                        <uui-button
                            label="Add Grader"
                            @click=${this._onAddGraderClick}>
                            Add Grader
                        </uui-button>

                        ${this._graders.length === 0
                            ? html`<p>No graders added. Add at least one grader to evaluate test results.</p>`
                            : html`
                                <div class="graders-list">
                                    ${this._graders.map((grader, index) => html`
                                        <div class="grader-item">
                                            <div class="grader-info">
                                                <span><strong>Grader:</strong> ${grader.graderId}</span>
                                                <span><strong>Weight:</strong> ${grader.weight}</span>
                                                <span><strong>Severity:</strong> ${grader.severity}</span>
                                            </div>
                                            <uui-button
                                                label="Remove"
                                                color="danger"
                                                @click=${() => this._onRemoveGraderClick(index)}>
                                                Remove
                                            </uui-button>
                                        </div>
                                    `)}
                                </div>
                            `}
                    </div>

                    <div class="form-section">
                        <h2>Target</h2>
                        <p>Target profile and contexts will be configured here.</p>
                        <!-- TODO: Add target configuration UI -->
                    </div>

                    <div class="form-section">
                        <h2>Test Case</h2>
                        <p>Test case configuration will be shown here based on test type.</p>
                        <!-- TODO: Add dynamic test case UI based on test type -->
                    </div>
                </div>
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: var(--uui-size-space-5);
        }

        .test-editor {
            max-width: 1200px;
        }

        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: var(--uui-size-space-5);
        }

        .actions {
            display: flex;
            gap: var(--uui-size-space-2);
        }

        .form {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-4);
        }

        .form-group {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }

        .form-group label {
            font-weight: bold;
        }

        .form-group small {
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .form-group select {
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            font-family: inherit;
            font-size: inherit;
        }

        .form-section {
            padding-top: var(--uui-size-space-4);
            border-top: 1px solid var(--uui-color-border);
        }

        .form-section h2 {
            margin: 0 0 var(--uui-size-space-3);
        }

        .graders-list {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-3);
            margin-top: var(--uui-size-space-3);
        }

        .grader-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
        }

        .grader-info {
            display: flex;
            gap: var(--uui-size-space-4);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'umbraco-ai-test-editor': UmbracoAITestEditorElement;
    }
}

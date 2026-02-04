import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { customElement, state } from '@umbraco-cms/backoffice/external/lit';
import { AITestRepository } from '../repository/test.repository.js';
import type { TestItemResponseModel } from '../../api/client/index.js';

/**
 * Root workspace element for AI Tests management.
 * Displays a list of tests with actions to create, edit, run, and delete.
 */
@customElement('umbraco-ai-tests-workspace-root')
export class UmbracoAITestsWorkspaceRootElement extends LitElement {
    @state()
    private _tests: TestItemResponseModel[] = [];

    @state()
    private _isLoading = true;

    @state()
    private _filter = '';

    private _repository = new AITestRepository(this);

    connectedCallback() {
        super.connectedCallback();
        this._loadTests();
    }

    private async _loadTests() {
        this._isLoading = true;
        try {
            const result = await this._repository.getAll(
                this._filter || undefined,
                undefined,
                0,
                100
            );
            this._tests = result.items;
        } catch (error) {
            console.error('Failed to load tests:', error);
        } finally {
            this._isLoading = false;
        }
    }

    private _onFilterChange(e: InputEvent) {
        const target = e.target as HTMLInputElement;
        this._filter = target.value;
        this._loadTests();
    }

    private _onCreateClick() {
        // TODO: Navigate to test create workspace
        console.log('Create test clicked');
    }

    private _onEditClick(test: TestItemResponseModel) {
        // TODO: Navigate to test edit workspace
        console.log('Edit test:', test.id);
    }

    private async _onRunClick(test: TestItemResponseModel) {
        try {
            const result = await this._repository.run(test.id);
            console.log('Test run result:', result);
            alert(`Test completed! Pass@k: ${result.passAtK.toFixed(2)}, Avg Score: ${result.averageScore.toFixed(2)}`);
        } catch (error) {
            console.error('Failed to run test:', error);
            alert('Failed to run test. See console for details.');
        }
    }

    private async _onDeleteClick(test: TestItemResponseModel) {
        if (!confirm(`Delete test "${test.name}"?`)) {
            return;
        }
        try {
            await this._repository.delete(test.id);
            await this._loadTests();
        } catch (error) {
            console.error('Failed to delete test:', error);
        }
    }

    render() {
        if (this._isLoading) {
            return html`<uui-loader></uui-loader>`;
        }

        return html`
            <div class="workspace-root">
                <div class="header">
                    <h1>AI Tests</h1>
                    <uui-button
                        label="Create Test"
                        look="primary"
                        color="positive"
                        @click=${this._onCreateClick}>
                        Create Test
                    </uui-button>
                </div>

                <div class="filters">
                    <uui-input
                        label="Filter"
                        placeholder="Search tests..."
                        .value=${this._filter}
                        @input=${this._onFilterChange}>
                    </uui-input>
                </div>

                <div class="test-list">
                    ${this._tests.length === 0
                        ? html`<p>No tests found. Create your first test!</p>`
                        : this._tests.map(test => html`
                            <div class="test-item">
                                <div class="test-info">
                                    <h3>${test.name}</h3>
                                    <p>${test.description || 'No description'}</p>
                                    <div class="test-meta">
                                        <span>Type: ${test.testTypeId}</span>
                                        <span>Runs: ${test.runCount}</span>
                                        ${test.tags.length > 0
                                            ? html`<span>Tags: ${test.tags.join(', ')}</span>`
                                            : ''}
                                    </div>
                                </div>
                                <div class="test-actions">
                                    <uui-button
                                        label="Run"
                                        look="primary"
                                        @click=${() => this._onRunClick(test)}>
                                        Run
                                    </uui-button>
                                    <uui-button
                                        label="Edit"
                                        @click=${() => this._onEditClick(test)}>
                                        Edit
                                    </uui-button>
                                    <uui-button
                                        label="Delete"
                                        color="danger"
                                        @click=${() => this._onDeleteClick(test)}>
                                        Delete
                                    </uui-button>
                                </div>
                            </div>
                        `)}
                </div>
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: var(--uui-size-space-5);
        }

        .workspace-root {
            max-width: 1200px;
        }

        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: var(--uui-size-space-5);
        }

        .filters {
            margin-bottom: var(--uui-size-space-4);
        }

        .test-list {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-3);
        }

        .test-item {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: var(--uui-size-space-4);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
        }

        .test-info h3 {
            margin: 0 0 var(--uui-size-space-2);
        }

        .test-info p {
            margin: 0 0 var(--uui-size-space-2);
            color: var(--uui-color-text-alt);
        }

        .test-meta {
            display: flex;
            gap: var(--uui-size-space-3);
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
        }

        .test-actions {
            display: flex;
            gap: var(--uui-size-space-2);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        'umbraco-ai-tests-workspace-root': UmbracoAITestsWorkspaceRootElement;
    }
}

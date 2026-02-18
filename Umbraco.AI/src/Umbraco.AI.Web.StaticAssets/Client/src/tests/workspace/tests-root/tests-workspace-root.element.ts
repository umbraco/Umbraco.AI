import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, state } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { AITestRepository } from "../../repository/test.repository.js";
import type { TestItemResponseModel } from "../../../api/types.gen.js";

/**
 * Root workspace element for AI Tests management.
 * Displays a list of tests with filtering, status indicators, and actions.
 */
@customElement("umbraco-ai-tests-workspace-root")
export class UmbracoAITestsWorkspaceRootElement extends UmbElementMixin(LitElement) {
    @state()
    private _tests: TestItemResponseModel[] = [];

    @state()
    private _isLoading = true;

    @state()
    private _filter = "";

    @state()
    private _tagFilter = "";

    @state()
    private _total = 0;

    private _repository!: AITestRepository;

    constructor() {
        super();
        this._repository = new AITestRepository(this);
    }

    connectedCallback() {
        super.connectedCallback();
        this._loadTests();
    }

    private async _loadTests() {
        this._isLoading = true;
        try {
            const result = await this._repository.getAllTests(
                this._filter || undefined,
                this._tagFilter || undefined,
                0,
                100
            );
            this._tests = result.items;
            this._total = result.total;
        } catch (error) {
            console.error("Failed to load tests:", error);
            this._tests = [];
            this._total = 0;
        } finally {
            this._isLoading = false;
        }
    }

    private _handleFilterChange(e: Event) {
        this._filter = (e.target as HTMLInputElement).value;
        this._loadTests();
    }

    private _handleTagFilterChange(e: Event) {
        this._tagFilter = (e.target as HTMLInputElement).value;
        this._loadTests();
    }

    private _handleCreate() {
        // Navigate to create workspace
        window.location.hash = "#/section/ai/workspace/test/create";
    }

    private _handleEdit(test: TestItemResponseModel) {
        // Navigate to edit workspace
        window.location.hash = `#/section/ai/workspace/test/edit/${test.id}`;
    }

    private async _handleRun(test: TestItemResponseModel) {
        try {
            const metrics = await this._repository.runTest(test.id);
            alert(
                `Test executed ${metrics.totalRuns} times\n` +
                `Passed: ${metrics.passedRuns}/${metrics.totalRuns}\n` +
                `pass@k: ${(metrics.passAtK * 100).toFixed(1)}%`
            );
            this._loadTests();
        } catch (error) {
            console.error("Failed to run test:", error);
            alert("Failed to run test. See console for details.");
        }
    }

    private async _handleDelete(test: TestItemResponseModel) {
        if (!confirm(`Delete test "${test.name}"?`)) {
            return;
        }

        try {
            await this._repository.deleteTest(test.id);
            this._loadTests();
        } catch (error) {
            console.error("Failed to delete test:", error);
            alert("Failed to delete test. See console for details.");
        }
    }

    private _renderTest(test: TestItemResponseModel) {
        return html`
            <tr>
                <td>${test.name}</td>
                <td>${test.alias}</td>
                <td>${test.testTypeId}</td>
                <td>
                    ${test.tags.map(tag => html`<span class="tag">${tag}</span>`)}
                </td>
                <td>${test.runCount}</td>
                <td class="actions">
                    <button @click=${() => this._handleEdit(test)} title="Edit">
                        ✏️
                    </button>
                    <button @click=${() => this._handleRun(test)} title="Run">
                        ▶️
                    </button>
                    <button @click=${() => this._handleDelete(test)} title="Delete">
                        🗑️
                    </button>
                </td>
            </tr>
        `;
    }

    render() {
        if (this._isLoading) {
            return html`<div class="loading">Loading tests...</div>`;
        }

        return html`
            <div class="container">
                <div class="header">
                    <h1>AI Tests</h1>
                    <button @click=${this._handleCreate} class="create-button">
                        + Create Test
                    </button>
                </div>

                <div class="filters">
                    <input
                        type="text"
                        placeholder="Filter by name..."
                        .value=${this._filter}
                        @input=${this._handleFilterChange}
                    />
                    <input
                        type="text"
                        placeholder="Filter by tags..."
                        .value=${this._tagFilter}
                        @input=${this._handleTagFilterChange}
                    />
                </div>

                <div class="results-info">
                    Showing ${this._tests.length} of ${this._total} tests
                </div>

                ${this._tests.length === 0
                    ? html`<div class="empty">No tests found. Create one to get started.</div>`
                    : html`
                        <table>
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Alias</th>
                                    <th>Test Type</th>
                                    <th>Tags</th>
                                    <th>Run Count</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                ${this._tests.map(test => this._renderTest(test))}
                            </tbody>
                        </table>
                    `}
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: 20px;
        }

        .loading,
        .empty {
            text-align: center;
            padding: 40px;
            color: var(--uui-color-text-alt);
        }

        .container {
            max-width: 1200px;
            margin: 0 auto;
        }

        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }

        h1 {
            margin: 0;
            font-size: 24px;
        }

        .create-button {
            padding: 10px 20px;
            background: var(--uui-color-positive);
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }

        .create-button:hover {
            background: var(--uui-color-positive-emphasis);
        }

        .filters {
            display: flex;
            gap: 10px;
            margin-bottom: 20px;
        }

        .filters input {
            flex: 1;
            padding: 8px 12px;
            border: 1px solid var(--uui-color-border);
            border-radius: 4px;
        }

        .results-info {
            margin-bottom: 10px;
            color: var(--uui-color-text-alt);
            font-size: 14px;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            background: var(--uui-color-surface);
            border-radius: 4px;
            overflow: hidden;
        }

        thead {
            background: var(--uui-color-surface-alt);
        }

        th,
        td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid var(--uui-color-border);
        }

        th {
            font-weight: 600;
            font-size: 14px;
        }

        tbody tr:hover {
            background: var(--uui-color-surface-emphasis);
        }

        .tag {
            display: inline-block;
            padding: 2px 8px;
            margin-right: 4px;
            background: var(--uui-color-surface-alt);
            border-radius: 3px;
            font-size: 12px;
        }

        .actions {
            white-space: nowrap;
        }

        .actions button {
            background: none;
            border: none;
            cursor: pointer;
            font-size: 18px;
            padding: 4px 8px;
            opacity: 0.7;
        }

        .actions button:hover {
            opacity: 1;
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-tests-workspace-root": UmbracoAITestsWorkspaceRootElement;
    }
}

export default UmbracoAITestsWorkspaceRootElement;

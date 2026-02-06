import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { customElement, property, state } from "@umbraco-cms/backoffice/external/lit";
import type {
    TestRunResponseModel,
    TestOutcomeResponseModel,
    TestTranscriptResponseModel,
    TestGraderResultResponseModel,
} from "../../api/client/index.js";

/**
 * Test run results viewer displaying transcripts, outcomes, and metrics.
 */
@customElement("umbraco-ai-test-run-results")
export class UmbracoAITestRunResultsElement extends LitElement {
    @property({ type: Object })
    testRun?: TestRunResponseModel;

    @state()
    private _expandedOutcomes = new Set<number>();

    private _toggleOutcome(index: number) {
        if (this._expandedOutcomes.has(index)) {
            this._expandedOutcomes.delete(index);
        } else {
            this._expandedOutcomes.add(index);
        }
        this.requestUpdate();
    }

    private _renderMetrics() {
        if (!this.testRun) return "";

        const passRate = (this.testRun.passAtK * 100).toFixed(1);
        const avgScore = this.testRun.averageScore.toFixed(2);

        return html`
            <div class="metrics">
                <div class="metric">
                    <span class="metric-label">Pass@k</span>
                    <span class="metric-value ${this.testRun.passAtK >= 0.8 ? "success" : "warning"}">
                        ${passRate}%
                    </span>
                </div>
                <div class="metric">
                    <span class="metric-label">Average Score</span>
                    <span class="metric-value">${avgScore}</span>
                </div>
                <div class="metric">
                    <span class="metric-label">Runs</span>
                    <span class="metric-value"> ${this.testRun.passedRuns} / ${this.testRun.totalRuns} </span>
                </div>
                <div class="metric">
                    <span class="metric-label">Duration</span>
                    <span class="metric-value">${this._formatDuration(this.testRun.totalDuration)}</span>
                </div>
            </div>
        `;
    }

    private _renderOutcomesList() {
        if (!this.testRun?.outcomes || this.testRun.outcomes.length === 0) {
            return html`<p>No outcomes available.</p>`;
        }

        return html`
            <div class="outcomes-list">
                ${this.testRun.outcomes.map((outcome, index) => this._renderOutcome(outcome, index))}
            </div>
        `;
    }

    private _renderOutcome(outcome: TestOutcomeResponseModel, index: number) {
        const isExpanded = this._expandedOutcomes.has(index);
        const statusIcon = outcome.passed ? "✓" : "✗";
        const statusClass = outcome.passed ? "passed" : "failed";

        return html`
            <div class="outcome-item ${statusClass}">
                <div class="outcome-header" @click=${() => this._toggleOutcome(index)}>
                    <span class="outcome-status">${statusIcon}</span>
                    <span class="outcome-title">Run ${index + 1}</span>
                    <span class="outcome-score">Score: ${outcome.averageScore.toFixed(2)}</span>
                    <span class="outcome-duration">${this._formatDuration(outcome.transcript.duration)}</span>
                    <span class="expand-icon">${isExpanded ? "▼" : "▶"}</span>
                </div>

                ${isExpanded
                    ? html`
                          <div class="outcome-details">
                              ${this._renderTranscript(outcome.transcript)}
                              ${this._renderGraderResults(outcome.graderResults)}
                          </div>
                      `
                    : ""}
            </div>
        `;
    }

    private _renderTranscript(transcript: TestTranscriptResponseModel) {
        return html`
            <div class="transcript">
                <h4>Transcript</h4>

                ${transcript.input
                    ? html`
                          <div class="transcript-section">
                              <strong>Input:</strong>
                              <pre>${this._formatJson(transcript.input)}</pre>
                          </div>
                      `
                    : ""}
                ${transcript.output
                    ? html`
                          <div class="transcript-section">
                              <strong>Output:</strong>
                              <pre>${this._formatJson(transcript.output)}</pre>
                          </div>
                      `
                    : ""}
                ${transcript.error
                    ? html`
                          <div class="transcript-section error">
                              <strong>Error:</strong>
                              <pre>${transcript.error}</pre>
                          </div>
                      `
                    : ""}
                ${transcript.metadata && Object.keys(transcript.metadata).length > 0
                    ? html`
                          <div class="transcript-section">
                              <strong>Metadata:</strong>
                              <pre>${this._formatJson(transcript.metadata)}</pre>
                          </div>
                      `
                    : ""}

                <div class="transcript-meta">
                    <span>Duration: ${this._formatDuration(transcript.duration)}</span>
                    ${transcript.tokenUsage
                        ? html`
                              <span
                                  >Tokens: ${transcript.tokenUsage.inputTokens}in /
                                  ${transcript.tokenUsage.outputTokens}out</span
                              >
                          `
                        : ""}
                </div>
            </div>
        `;
    }

    private _renderGraderResults(results: TestGraderResultResponseModel[]) {
        if (!results || results.length === 0) {
            return "";
        }

        return html`
            <div class="grader-results">
                <h4>Grader Results</h4>
                ${results.map((result) => this._renderGraderResult(result))}
            </div>
        `;
    }

    private _renderGraderResult(result: TestGraderResultResponseModel) {
        const statusIcon = result.passed ? "✓" : "✗";
        const statusClass = result.passed ? "passed" : "failed";

        return html`
            <div class="grader-result ${statusClass}">
                <div class="grader-result-header">
                    <span class="grader-status">${statusIcon}</span>
                    <span class="grader-name">${result.graderId}</span>
                    <span class="grader-score">Score: ${result.score.toFixed(2)}</span>
                    <span class="grader-severity severity-${result.severity}">${result.severity}</span>
                </div>

                ${result.reason
                    ? html` <div class="grader-reason"><strong>Reason:</strong> ${result.reason}</div> `
                    : ""}
                ${result.details && Object.keys(result.details).length > 0
                    ? html`
                          <div class="grader-details">
                              <strong>Details:</strong>
                              <pre>${this._formatJson(result.details)}</pre>
                          </div>
                      `
                    : ""}
            </div>
        `;
    }

    private _formatDuration(ms: number): string {
        if (ms < 1000) {
            return `${ms.toFixed(0)}ms`;
        } else if (ms < 60000) {
            return `${(ms / 1000).toFixed(1)}s`;
        } else {
            const minutes = Math.floor(ms / 60000);
            const seconds = ((ms % 60000) / 1000).toFixed(0);
            return `${minutes}m ${seconds}s`;
        }
    }

    private _formatJson(obj: any): string {
        try {
            return JSON.stringify(obj, null, 2);
        } catch {
            return String(obj);
        }
    }

    render() {
        if (!this.testRun) {
            return html`<p>No test run data available.</p>`;
        }

        return html`
            <div class="test-run-results">
                <div class="header">
                    <h2>Test Run Results</h2>
                    <div class="test-info">
                        <span><strong>Test:</strong> ${this.testRun.testAlias}</span>
                        <span><strong>Completed:</strong> ${new Date(this.testRun.completedAt).toLocaleString()}</span>
                    </div>
                </div>

                ${this._renderMetrics()}

                <div class="outcomes-section">
                    <h3>Run Outcomes (${this.testRun.totalRuns})</h3>
                    ${this._renderOutcomesList()}
                </div>
            </div>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: var(--uui-size-space-4);
        }

        .test-run-results {
            max-width: 1200px;
        }

        .header {
            margin-bottom: var(--uui-size-space-4);
        }

        .test-info {
            display: flex;
            gap: var(--uui-size-space-4);
            margin-top: var(--uui-size-space-2);
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .metrics {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-5);
        }

        .metric {
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-2);
        }

        .metric-label {
            font-size: var(--uui-type-small-size);
            color: var(--uui-color-text-alt);
        }

        .metric-value {
            font-size: var(--uui-size-6);
            font-weight: bold;
        }

        .metric-value.success {
            color: var(--uui-color-positive);
        }

        .metric-value.warning {
            color: var(--uui-color-warning);
        }

        .outcomes-section {
            margin-top: var(--uui-size-space-5);
        }

        .outcomes-list {
            display: flex;
            flex-direction: column;
            gap: var(--uui-size-space-3);
            margin-top: var(--uui-size-space-3);
        }

        .outcome-item {
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            background: var(--uui-color-surface);
            overflow: hidden;
        }

        .outcome-item.passed {
            border-left: 4px solid var(--uui-color-positive);
        }

        .outcome-item.failed {
            border-left: 4px solid var(--uui-color-danger);
        }

        .outcome-header {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
            padding: var(--uui-size-space-3);
            cursor: pointer;
            user-select: none;
        }

        .outcome-header:hover {
            background: var(--uui-color-surface-emphasis);
        }

        .outcome-status {
            font-size: var(--uui-size-5);
            font-weight: bold;
        }

        .outcome-item.passed .outcome-status {
            color: var(--uui-color-positive);
        }

        .outcome-item.failed .outcome-status {
            color: var(--uui-color-danger);
        }

        .outcome-title {
            font-weight: bold;
            flex: 1;
        }

        .outcome-score,
        .outcome-duration {
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .expand-icon {
            color: var(--uui-color-text-alt);
        }

        .outcome-details {
            padding: var(--uui-size-space-4);
            border-top: 1px solid var(--uui-color-border);
            background: var(--uui-color-surface-alt);
        }

        .transcript,
        .grader-results {
            margin-bottom: var(--uui-size-space-4);
        }

        .transcript h4,
        .grader-results h4 {
            margin: 0 0 var(--uui-size-space-3);
        }

        .transcript-section {
            margin-bottom: var(--uui-size-space-3);
        }

        .transcript-section.error {
            color: var(--uui-color-danger);
        }

        .transcript-section pre,
        .grader-details pre {
            padding: var(--uui-size-space-3);
            background: var(--uui-color-surface);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            overflow-x: auto;
            font-size: var(--uui-type-small-size);
            margin: var(--uui-size-space-2) 0 0;
        }

        .transcript-meta {
            display: flex;
            gap: var(--uui-size-space-4);
            margin-top: var(--uui-size-space-3);
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .grader-result {
            padding: var(--uui-size-space-3);
            border: 1px solid var(--uui-color-border);
            border-radius: var(--uui-border-radius);
            margin-bottom: var(--uui-size-space-3);
        }

        .grader-result.passed {
            border-left: 3px solid var(--uui-color-positive);
        }

        .grader-result.failed {
            border-left: 3px solid var(--uui-color-danger);
        }

        .grader-result-header {
            display: flex;
            align-items: center;
            gap: var(--uui-size-space-3);
            margin-bottom: var(--uui-size-space-2);
        }

        .grader-status {
            font-size: var(--uui-size-4);
            font-weight: bold;
        }

        .grader-result.passed .grader-status {
            color: var(--uui-color-positive);
        }

        .grader-result.failed .grader-status {
            color: var(--uui-color-danger);
        }

        .grader-name {
            font-weight: bold;
            flex: 1;
        }

        .grader-score {
            color: var(--uui-color-text-alt);
            font-size: var(--uui-type-small-size);
        }

        .grader-severity {
            padding: 2px 8px;
            border-radius: var(--uui-border-radius);
            font-size: var(--uui-type-small-size);
            font-weight: bold;
        }

        .severity-error {
            background: var(--uui-color-danger-emphasis);
            color: var(--uui-color-danger-contrast);
        }

        .severity-warning {
            background: var(--uui-color-warning-emphasis);
            color: var(--uui-color-warning-contrast);
        }

        .severity-info {
            background: var(--uui-color-default-emphasis);
            color: var(--uui-color-default-contrast);
        }

        .grader-reason {
            margin-top: var(--uui-size-space-2);
            color: var(--uui-color-text-alt);
        }

        .grader-details {
            margin-top: var(--uui-size-space-2);
        }
    `;
}

declare global {
    interface HTMLElementTagNameMap {
        "umbraco-ai-test-run-results": UmbracoAITestRunResultsElement;
    }
}

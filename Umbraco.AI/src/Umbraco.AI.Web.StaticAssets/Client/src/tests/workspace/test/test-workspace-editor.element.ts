import { LitElement, html, customElement } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";

/**
 * Workspace editor element for tests.
 * This is a wrapper that renders the umb-router-slot for workspace views.
 */
@customElement("uai-test-workspace-editor")
export class UaiTestWorkspaceEditorElement extends UmbElementMixin(LitElement) {
	render() {
		return html`<umb-router-slot id="router-slot"></umb-router-slot>`;
	}
}

export default UaiTestWorkspaceEditorElement;

declare global {
	interface HTMLElementTagNameMap {
		"uai-test-workspace-editor": UaiTestWorkspaceEditorElement;
	}
}

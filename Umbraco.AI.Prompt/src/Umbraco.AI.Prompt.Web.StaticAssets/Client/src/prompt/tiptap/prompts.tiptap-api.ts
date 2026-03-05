import { UmbTiptapExtensionApiBase } from '@umbraco-cms/backoffice/tiptap';
import type { Editor } from '@umbraco-cms/backoffice/tiptap';
import { UAI_TIPTAP_TOOLBAR_PROMPTS_ALIAS } from './constants.js';

/**
 * TipTap extension API for AI Prompts.
 * No TipTap editor extensions are needed (no new marks/nodes).
 * Auto-injects the toolbar button alias into the toolbar layout.
 */
export class UaiPromptsTiptapApi extends UmbTiptapExtensionApiBase {
    getTiptapExtensions() {
        return [];
    }

    override setEditor(editor: Editor) {
        super.setEditor(editor);
        this.#injectToolbarAlias();
    }

    #injectToolbarAlias() {
        // Walk up from the host element to find the umb-input-tiptap element
        const host = this.getHostElement();
        const inputTiptap = host?.closest('umb-input-tiptap') as any;
        if (!inputTiptap) return;

        // Find the toolbar element within the input-tiptap shadow DOM
        const toolbar = inputTiptap.shadowRoot?.querySelector('umb-tiptap-toolbar') as any;
        if (!toolbar) return;

        const currentToolbar = toolbar.toolbar;
        if (!currentToolbar || !Array.isArray(currentToolbar)) return;

        // Check if already injected
        if (currentToolbar.flat(2).includes(UAI_TIPTAP_TOOLBAR_PROMPTS_ALIAS)) return;

        // Clone and add as a new group at the end of the last row
        const newToolbar = structuredClone(currentToolbar);
        const lastRow = newToolbar[newToolbar.length - 1];
        if (lastRow) {
            lastRow.push([UAI_TIPTAP_TOOLBAR_PROMPTS_ALIAS]);
        } else {
            newToolbar.push([[UAI_TIPTAP_TOOLBAR_PROMPTS_ALIAS]]);
        }
        toolbar.toolbar = newToolbar;
    }
}

export default UaiPromptsTiptapApi;

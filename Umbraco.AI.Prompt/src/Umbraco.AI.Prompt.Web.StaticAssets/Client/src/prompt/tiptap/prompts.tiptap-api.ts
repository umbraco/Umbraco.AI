import { UmbTiptapExtensionApiBase } from '@umbraco-cms/backoffice/tiptap';

/**
 * TipTap extension API for AI Prompts.
 * No TipTap editor extensions are needed (no new marks/nodes).
 * The toolbar button must be manually placed in the RTE toolbar layout.
 */
export class UaiPromptsTiptapApi extends UmbTiptapExtensionApiBase {
    getTiptapExtensions() {
        return [];
    }
}

export default UaiPromptsTiptapApi;

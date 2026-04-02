import { UmbTiptapExtensionApiBase } from '@umbraco-cms/backoffice/tiptap';

/**
 * TipTap extension API for AI Dictate.
 * No TipTap editor extensions are needed (no new marks/nodes).
 * The toolbar button must be manually placed in the RTE toolbar layout.
 */
export class UaiDictateTiptapApi extends UmbTiptapExtensionApiBase {
    getTiptapExtensions() {
        return [];
    }
}

export default UaiDictateTiptapApi;

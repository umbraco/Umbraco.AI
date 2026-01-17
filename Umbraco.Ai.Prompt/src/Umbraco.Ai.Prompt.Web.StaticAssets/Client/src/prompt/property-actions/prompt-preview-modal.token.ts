import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { UaiPromptPreviewModalData, UaiPromptPreviewModalValue } from './types.js';

export const UAI_PROMPT_PREVIEW_MODAL_ALIAS = 'UmbracoAiPrompt.Modal.PromptPreview';

export const UAI_PROMPT_PREVIEW_MODAL = new UmbModalToken<
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue
>(UAI_PROMPT_PREVIEW_MODAL_ALIAS, {
    modal: {
        type: 'dialog',
        size: 'medium',
    },
});

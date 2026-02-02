import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { UaiPromptPreviewModalData, UaiPromptPreviewModalValue } from './types.js';

export const UAI_PROMPT_PREVIEW_MODAL_ALIAS = 'UmbracoAIPrompt.Modal.PromptPreview';

/**
 * Modal token for the centered dialog version.
 */
export const UAI_PROMPT_PREVIEW_MODAL = new UmbModalToken<
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue
>(UAI_PROMPT_PREVIEW_MODAL_ALIAS, {
    modal: {
        type: 'dialog',
        size: 'medium',
    },
});

/**
 * Modal token for the sidebar/panel version.
 * Uses the same element but renders as a slide-in sidebar.
 */
export const UAI_PROMPT_PREVIEW_SIDEBAR = new UmbModalToken<
    UaiPromptPreviewModalData,
    UaiPromptPreviewModalValue
>(UAI_PROMPT_PREVIEW_MODAL_ALIAS, {
    modal: {
        type: 'sidebar',
        size: 'small',
    },
});

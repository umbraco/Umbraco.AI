import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { UAiAgentPreviewModalData, UAiAgentPreviewModalValue } from './types.js';

export const UAI_PROMPT_PREVIEW_MODAL_ALIAS = 'UmbracoAiAgent.Modal.PromptPreview';

export const UAI_PROMPT_PREVIEW_MODAL = new UmbModalToken<
    UAiAgentPreviewModalData,
    UAiAgentPreviewModalValue
>(UAI_PROMPT_PREVIEW_MODAL_ALIAS, {
    modal: {
        type: 'dialog',
        size: 'medium',
    },
});

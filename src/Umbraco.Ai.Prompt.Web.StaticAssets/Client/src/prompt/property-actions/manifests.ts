import type { ManifestModal } from '@umbraco-cms/backoffice/modal';
import { UAI_PROMPT_PREVIEW_MODAL_ALIAS } from './prompt-preview-modal.token.js';

/**
 * Static manifests for the prompt property actions feature.
 * Note: Individual prompt property actions are registered dynamically by the registrar controller.
 */
export const promptPropertyActionManifests: Array<ManifestModal> = [
    {
        type: 'modal',
        alias: UAI_PROMPT_PREVIEW_MODAL_ALIAS,
        name: 'Prompt Preview Modal',
        element: () => import('./prompt-preview-modal.element.js'),
    },
];

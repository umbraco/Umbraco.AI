import { UAI_PROMPT_PREVIEW_MODAL_ALIAS } from './prompt-preview-modal.token.js';
import { UAI_PROMPT_SCOPE_CONDITION_ALIAS } from './constants.js';

/**
 * Static manifests for the prompt property actions feature.
 * Note: Individual prompt property actions are registered dynamically by the registrar controller.
 */
export const promptPropertyActionManifests: Array<UmbExtensionManifest> = [
    {
        type: 'modal',
        alias: UAI_PROMPT_PREVIEW_MODAL_ALIAS,
        name: 'Prompt Preview Modal',
        element: () => import('./prompt-preview-modal.element.js'),
    },
    {
        type: 'condition',
        alias: UAI_PROMPT_SCOPE_CONDITION_ALIAS,
        name: 'Prompt Scope Condition',
        api: () => import('./prompt-scope.condition.js'),
    },
];

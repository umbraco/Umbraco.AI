import type { ManifestPropertyAction } from '@umbraco-cms/backoffice/property-action';
import { TEXT_BASED_PROPERTY_EDITOR_UIS, UAI_PROMPT_PROPERTY_ACTION_PREFIX } from './constants.js';
import { UAI_PROMPT_ICON } from '../constants.js';
import type { UaiPromptRegistrationModel, UaiPromptPropertyActionMeta } from './types.js';

/**
 * Generates a property action manifest for a prompt.
 * The manifest registers a property action that will appear on text-based property editors.
 */
export function generatePromptPropertyActionManifest(
    prompt: UaiPromptRegistrationModel,
    weight: number = 100
): ManifestPropertyAction<UaiPromptPropertyActionMeta> {
    return {
        type: 'propertyAction',
        kind: 'default',
        alias: `${UAI_PROMPT_PROPERTY_ACTION_PREFIX}.${prompt.alias}`,
        name: `Insert Prompt: ${prompt.name}`,
        forPropertyEditorUis: [...TEXT_BASED_PROPERTY_EDITOR_UIS],
        api: () => import('./prompt-insert.property-action.js'),
        weight,
        meta: {
            icon: UAI_PROMPT_ICON,
            label: prompt.name,
            promptUnique: prompt.unique,
            promptAlias: prompt.alias,
            promptContent: prompt.content,
            promptDescription: prompt.description,
        },
    };
}
